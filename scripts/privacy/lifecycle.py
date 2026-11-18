#!/usr/bin/env python3
"""Installation-owner retention journal for database records and managed file copies."""
import argparse
import contextlib
import datetime as dt
import hashlib
import json
import os
from pathlib import Path
import sqlite3
import stat
import subprocess
import tempfile
import uuid

# These are secondary copies. A live domain object retains its domain ACL and deletion path.
FILES = ('operational-log', 'test-result', 'private-evidence', 'backup', 'attachment-copy')
# The psql service identifies a database installation, never a customer-selected URL.
DATABASES = {
    'audit': ('product', 'audit_logs', 'created_at', 'tenant_id', 'actor_id'),
}
SCHEMA = """
CREATE TABLE IF NOT EXISTS vault_identity(singleton INTEGER PRIMARY KEY CHECK(singleton=1), id TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS policies(store TEXT PRIMARY KEY, days INTEGER NOT NULL, review TEXT NOT NULL, purpose TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS holds(id TEXT PRIMARY KEY, store TEXT NOT NULL, tenant TEXT NOT NULL, subject TEXT, review TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS requests(id TEXT PRIMARY KEY, tenant TEXT NOT NULL, subject TEXT NOT NULL, kind TEXT NOT NULL, status TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS objects(id TEXT PRIMARY KEY, store TEXT NOT NULL, tenant TEXT NOT NULL, subject TEXT, created TEXT NOT NULL, size INTEGER NOT NULL, digest TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS result_files(id TEXT PRIMARY KEY, filename TEXT NOT NULL, expires TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS plans(id TEXT PRIMARY KEY, store TEXT NOT NULL, tenant TEXT NOT NULL, subject TEXT, cutoff TEXT NOT NULL, request TEXT, state TEXT NOT NULL, created TEXT NOT NULL, days INTEGER NOT NULL);
CREATE TABLE IF NOT EXISTS items(plan TEXT NOT NULL, id TEXT NOT NULL, digest TEXT NOT NULL, size INTEGER NOT NULL, PRIMARY KEY(plan,id));
"""
def now():
    return dt.datetime.now(dt.timezone.utc)

def guid(value):
    return str(uuid.UUID(value))

def day(value):
    return dt.date.fromisoformat(value).isoformat()

def private_directory(path):
    if not path.is_absolute() or path.resolve() != path:
        raise ValueError('Use a dedicated absolute path without links')
    path.mkdir(mode=0o700, parents=True, exist_ok=True)
    st = path.stat()
    if st.st_uid != os.getuid() or st.st_mode & 0o077:
        raise ValueError('Journal and vault must be owner-only directories')

class Journal:
    def __init__(self, path, vault, services, db_runner=None, databases=None):
        private_directory(path)
        private_directory(vault)
        if path == vault or path in vault.parents or vault in path.parents:
            raise ValueError('Keep the journal and managed copies in separate directories')
        self.vault, self.services = vault, services
        self.databases = databases or DATABASES
        self.run_sql = db_runner or self.psql
        self.db = sqlite3.connect(path / 'lifecycle.sqlite', timeout=30, isolation_level=None)
        self.db.row_factory = sqlite3.Row
        self.db.executescript(SCHEMA)
        marker=vault/'.sddp-vault-id'
        with self.lock():
            saved=self.db.execute('SELECT id FROM vault_identity WHERE singleton=1').fetchone()
            if marker.is_symlink(): raise ValueError('Unexpected vault identity link')
            if saved:
                if not marker.is_file() or marker.read_text()!=saved[0]: raise ValueError('Vault and journal do not match')
            else:
                if marker.exists(): raise ValueError('Existing vault requires its original journal')
                key=str(uuid.uuid4())
                with marker.open('x') as file:
                    file.write(key)
                    file.flush()
                    os.fsync(file.fileno())
                self.db.execute('INSERT INTO vault_identity VALUES(1,?)',(key,))

    @contextlib.contextmanager
    def lock(self):
        self.db.execute('BEGIN IMMEDIATE')
        try:
            yield
            self.db.commit()
        except BaseException:
            self.db.rollback()
            raise

    def psql(self, service, sql):
        if not self.services.get(service):
            raise ValueError(f'Configure the {service} database service')
        env = dict(os.environ, PGSERVICE=self.services[service], PGCONNECT_TIMEOUT='5')
        result = subprocess.run(['psql','-X','-qAt','-v','ON_ERROR_STOP=1'],
                                input="SET TIME ZONE 'UTC';\n" + sql, text=True, capture_output=True, env=env, timeout=120)
        if result.returncode:
            raise RuntimeError('Database operation failed; journal retained')
        return result.stdout.splitlines()

    def ingest(self, store, tenant, subject, source):
        key = str(uuid.uuid4())
        digest = hashlib.sha256()
        with source.open('rb') as contents, tempfile.NamedTemporaryFile(dir=self.vault, delete=False) as copy:
            try:
                while block := contents.read(1024*1024):
                    copy.write(block)
                    digest.update(block)
                copy.flush()
                os.fsync(copy.fileno())
                size = copy.tell()
                os.rename(copy.name, self.vault / key)
                with self.lock():
                    self.db.execute('INSERT INTO objects VALUES(?,?,?,?,?,?,?)',
                                    (key,store,tenant,subject,now().isoformat(),size,digest.hexdigest()))
            finally:
                Path(copy.name).unlink(missing_ok=True)
        return {'copy': key, 'bytes': size}

    def register_result(self, job, tenant, subject, raw, expires):
        key=guid(job)
        filename=f'{int(expires.timestamp())}-{key}.trx'
        file=self.vault/filename
        digest=hashlib.sha256(raw).hexdigest()
        with self.lock():
            with file.open('xb') as output:
                output.write(raw)
                output.flush()
                os.fsync(output.fileno())
            self.db.execute('INSERT INTO objects VALUES(?,?,?,?,?,?,?)',
                (key,'test-result',guid(tenant),guid(subject) if subject else None,now().isoformat(),len(raw),digest))
            self.db.execute('INSERT INTO result_files VALUES(?,?,?)',(key,filename,expires.isoformat()))
        return digest

    def expire_results(self):
        # The installation-approved target's raw retention authorizes expiry; holds still win.
        due=self.db.execute("""SELECT o.*,r.expires FROM objects o JOIN result_files r USING(id)
            WHERE r.expires<=? ORDER BY r.expires LIMIT 100""",(now().isoformat(),)).fetchall()
        for row in due:
            if self.held('test-result',row['tenant'],row['subject']): continue
            plan=str(uuid.uuid4())
            with self.lock():
                self.db.execute('INSERT INTO plans VALUES(?,?,?,?,?,?,?,?,?)',
                    (plan,'test-result',row['tenant'],row['subject'],row['expires'],None,'pending',now().isoformat(),0))
                self.db.execute('INSERT INTO items VALUES(?,?,?,?)',(plan,row['id'],row['digest'],row['size']))
            self.apply(plan)

    def managed_path(self,key):
        key=guid(key)
        row=self.db.execute('SELECT filename FROM result_files WHERE id=?',(key,)).fetchone()
        if row is None: return self.vault/key
        filename=row[0]
        stamp,_,suffix=filename.partition('-')
        if not stamp.isdecimal() or suffix!=key+'.trx': raise ValueError('Invalid managed result path')
        return self.vault/filename

    def inventory(self, store, tenant, subject, cutoff, after=None):
        if store in FILES:
            return [dict(r) for r in self.db.execute(
                'SELECT id,digest,size FROM objects WHERE store=? AND tenant=? AND created<=? AND (? IS NULL OR subject=?) AND (? IS NULL OR id>?) ORDER BY id LIMIT 1001',
                (store,tenant,cutoff,subject,subject,after,after))]
        service, table, timestamp, scope, actor = self.databases[store]
        # All interpolated values are typed by this boundary, all identifiers are constants.
        where = f"{scope}::text='{guid(tenant)}' AND {timestamp} <= '{dt.datetime.fromisoformat(cutoff).isoformat()}'::timestamptz"
        if after:
            where += f" AND id>'{guid(after)}'::uuid"
        if subject:
            where += f" AND {actor}::text='{guid(subject)}'"
        sql = f"""SELECT json_build_object('id',id,'size',pg_column_size(t),'digest',
            encode(sha256(convert_to(to_jsonb(t)::text,'UTF8')),'hex')) FROM {table} t
            WHERE {where} ORDER BY id LIMIT 1001;"""
        return [json.loads(row) for row in self.run_sql(service, sql)]

    def preview(self, store, tenant, request):
        with self.lock():
            policy = self.db.execute('SELECT * FROM policies WHERE store=?',(store,)).fetchone()
            if not policy or policy['review'] < now().date().isoformat():
                raise ValueError('A reviewed retention policy is required')
            subject = None
            cutoff = now() - dt.timedelta(days=policy['days'])
            if request:
                row = self.db.execute("SELECT subject FROM requests WHERE id=? AND tenant=? AND status='reviewed' AND kind='erase'",(request,tenant)).fetchone()
                if not row: raise ValueError('Review the rights request first')
                subject, cutoff = row[0], now()
            items = self.inventory(store,tenant,subject,cutoff.isoformat())
            key = str(uuid.uuid4())
            self.db.execute('INSERT INTO plans VALUES(?,?,?,?,?,?,?,?,?)',(key,store,tenant,subject,cutoff.isoformat(),request,'preview',now().isoformat(),policy['days']))
            self.db.executemany('INSERT INTO items VALUES(?,?,?,?)',[(key,i['id'],i['digest'],i['size']) for i in items[:1000]])
            return {'plan':key,'count':min(len(items),1000),'bytes':sum(i['size'] for i in items[:1000]),'more':len(items)>1000,
                    'holds':self.held(store,tenant,subject)}

    def held(self, store, tenant, subject):
        # A subject hold also blocks an unfiltered batch, even after its review date.
        return self.db.execute('SELECT count(*) FROM holds WHERE store=? AND tenant=? AND (subject IS NULL OR ? IS NULL OR subject=?)',
                               (store,tenant,subject,subject)).fetchone()[0]

    def apply(self, key, restore=False):
        # Commit a replayable deletion intent outside the database/backup being maintained.
        with self.lock():
            plan = self.db.execute('SELECT * FROM plans WHERE id=?',(key,)).fetchone()
            if not plan: raise ValueError('Unknown plan')
            if plan['state']=='applied' and not restore: return {'status':'applied'}
            if restore and plan['state'] not in ('pending','applied'): raise ValueError('Only authorized deletions can be replayed')
            if self.held(plan['store'],plan['tenant'],plan['subject']): raise ValueError('Legal hold requires review')
            if plan['state']=='preview':
                policy = self.db.execute('SELECT * FROM policies WHERE store=?',(plan['store'],)).fetchone()
                if not policy or policy['days']!=plan['days'] or policy['review']<now().date().isoformat() or now()-dt.datetime.fromisoformat(plan['created'])>dt.timedelta(hours=1):
                    raise ValueError('Policy or preview expired; preview again')
                self.db.execute("UPDATE plans SET state='pending' WHERE id=?",(key,))
        with self.lock():
            if self.held(plan['store'],plan['tenant'],plan['subject']): raise ValueError('Legal hold requires review')
            items = self.db.execute('SELECT * FROM items WHERE plan=? ORDER BY id',(key,)).fetchall()
            if plan['store'] in FILES:
                for item in items:
                    p = self.managed_path(item['id'])
                    if p.is_symlink(): raise ValueError('Managed file replaced')
                    if p.exists():
                        with p.open('rb') as f:
                            st = os.fstat(f.fileno())
                            if st.st_nlink != 1 or not stat.S_ISREG(st.st_mode) or hashlib.file_digest(f,'sha256').hexdigest()!=item['digest']:
                                raise ValueError('Managed file changed since preview')
                        p.unlink()
                    self.db.execute('DELETE FROM objects WHERE id=? AND tenant=?',(item['id'],plan['tenant']))
            elif items:
                service,table,_,scope,_ = self.databases[plan['store']]
                checks=[]
                for i in items:
                    digest=bytes.fromhex(i['digest']).hex()
                    checks.append(f"('{guid(i['id'])}'::uuid,'{digest}')")
                values=','.join(checks)
                tenant=guid(plan['tenant'])
                # Locks selected rows and refuses a changed record before any deletion commits.
                self.run_sql(service,f"""BEGIN; SET LOCAL lock_timeout='5s';
                CREATE TEMP TABLE batch(id uuid PRIMARY KEY,digest text) ON COMMIT DROP;
                INSERT INTO batch VALUES {values};
                DO $$ BEGIN
                PERFORM t.id FROM {table} t JOIN batch b USING(id) FOR UPDATE OF t;
                IF EXISTS(SELECT 1 FROM {table} t JOIN batch b USING(id)
                    WHERE t.{scope}::text IS DISTINCT FROM '{tenant}' OR
                    encode(sha256(convert_to(to_jsonb(t)::text,'UTF8')),'hex')<>b.digest)
                THEN RAISE EXCEPTION 'Reviewed record changed'; END IF;
                END $$;
                DELETE FROM {table} t USING batch b WHERE t.id=b.id AND t.{scope}::text='{tenant}'; COMMIT;""")
            self.db.execute("UPDATE plans SET state='applied' WHERE id=?",(key,))
            return {'status':'applied','reviewed':len(items)}

def cli(databases=DATABASES):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--journal',type=Path,required=True)
    parser.add_argument('--vault',type=Path,required=True)
    parser.add_argument('--metrics-dir',type=Path,help='Dedicated owner-only directory for the aggregate operational snapshot')
    services = sorted({row[0] for row in databases.values()})
    for service in services:
        parser.add_argument(f'--{service}-service', help=f'libpq service for the {service} database')
    commands = parser.add_subparsers(dest='command',required=True)
    stores = (*FILES,*databases)
    def scoped(name, allowed=stores):
        sub=commands.add_parser(name)
        sub.add_argument('store', choices=allowed)
        sub.add_argument('tenant',type=guid)
        return sub
    policy=commands.add_parser('policy')
    policy.add_argument('store',choices=stores)
    policy.add_argument('days',type=int)
    policy.add_argument('--purpose',required=True)
    policy.add_argument('--review',type=day,required=True)
    ingest=scoped('ingest',FILES)
    ingest.add_argument('file',type=Path)
    ingest.add_argument('--subject',type=guid)
    hold=scoped('hold')
    hold.add_argument('--subject',type=guid)
    hold.add_argument('--review',type=day,required=True)
    commands.add_parser('release-hold').add_argument('id',type=guid)
    request=commands.add_parser('request')
    request.add_argument('tenant',type=guid)
    request.add_argument('subject',type=guid)
    request.add_argument('kind',choices=['access','erase','restrict'])
    review=commands.add_parser('review-request')
    review.add_argument('id',type=guid)
    review.add_argument('--review',type=day,required=True)
    access=commands.add_parser('locate')
    access.add_argument('id',type=guid)
    access.add_argument('store',choices=stores)
    access.add_argument('--after',type=guid,help='Last record id of the preceding page')
    preview=scoped('preview')
    preview.add_argument('--request',type=guid)
    commands.add_parser('apply').add_argument('plan',type=guid)
    commands.add_parser('restore-replay')
    commands.add_parser('reviews')
    args=parser.parse_args()
    os.umask(0o077)
    ledger=Journal(args.journal,args.vault,{s:getattr(args,s+'_service') for s in services},databases=databases)
    if args.metrics_dir:
        private_directory(args.metrics_dir)
        if args.metrics_dir in (args.journal,args.vault): parser.error('Use a separate operational metrics directory')
    try:
        out = execute(args,ledger,stores)
    finally:
        if args.metrics_dir:
            publish_metrics(ledger,args.metrics_dir)
    print(json.dumps(out))

def publish_metrics(ledger, directory):
    # No identifiers, payloads, dates of individual requests, or file names in the export.
    with ledger.lock():
        db=ledger.db
        counts={
            'openHolds':db.execute('SELECT COUNT(*) FROM holds').fetchone()[0],
            'requestsAwaitingReview':db.execute("SELECT COUNT(*) FROM requests WHERE status='received'").fetchone()[0],
            'pendingDeletionBatches':db.execute("SELECT COUNT(*) FROM plans WHERE state='pending'").fetchone()[0],
            'reviewedDeletions':db.execute("SELECT COUNT(*) FROM items JOIN plans ON plans.id=items.plan WHERE plans.state='applied'").fetchone()[0],
        }
        with tempfile.NamedTemporaryFile(dir=directory,mode='w',delete=False) as tmp:
            try:
                json.dump(counts,tmp)
                tmp.flush()
                os.fsync(tmp.fileno())
                os.replace(tmp.name,directory/'privacy-counts.json')
            finally:
                Path(tmp.name).unlink(missing_ok=True)

def execute(args,ledger,stores):
    db=ledger.db
    if args.command=='policy':
        if not (1 <= args.days <= 36500) or not args.purpose.strip() or args.review < now().date().isoformat():
            raise ValueError('Provide a positive period, purpose and current policy review')
        with ledger.lock():
            db.execute('INSERT OR REPLACE INTO policies VALUES(?,?,?,?)',(args.store,args.days,args.review,args.purpose))
        out={'policy':args.store}
    elif args.command=='ingest':
        out=ledger.ingest(args.store,args.tenant,args.subject,args.file)
    elif args.command=='hold':
        key=str(uuid.uuid4())
        with ledger.lock(): db.execute('INSERT INTO holds VALUES(?,?,?,?,?)',(key,args.store,args.tenant,args.subject,args.review))
        out={'hold':key}
    elif args.command=='release-hold':
        with ledger.lock():
            if not db.execute('DELETE FROM holds WHERE id=?',(args.id,)).rowcount: raise ValueError('Unknown hold')
        out={'released':args.id}
    elif args.command=='request':
        key=str(uuid.uuid4())
        with ledger.lock(): db.execute("INSERT INTO requests VALUES(?,?,?,?,'received')",(key,args.tenant,args.subject,args.kind))
        out={'request':key}
    elif args.command=='review-request':
        with ledger.lock():
            r=db.execute('SELECT * FROM requests WHERE id=?',(args.id,)).fetchone()
            if not r: raise ValueError('Unknown request')
            if r['status']=='received' and r['kind']=='restrict':
                db.executemany('INSERT INTO holds VALUES(?,?,?,?,?)',[(str(uuid.uuid4()),s,r['tenant'],r['subject'],args.review) for s in stores])
            db.execute("UPDATE requests SET status='reviewed' WHERE id=?",(args.id,))
        out={'reviewed':args.id}
    elif args.command=='locate':
        r=db.execute("SELECT * FROM requests WHERE id=? AND kind='access' AND status='reviewed'",(args.id,)).fetchone()
        if not r: raise ValueError('Review the access request first')
        entries=ledger.inventory(args.store,r['tenant'],r['subject'],now().isoformat(),args.after)
        out={'records':[i['id'] for i in entries[:1000]],'next':entries[999]['id'] if len(entries)>1000 else None}
    elif args.command=='preview':
        out=ledger.preview(args.store,args.tenant,args.request)
    elif args.command=='apply':
        out=ledger.apply(args.plan)
    elif args.command=='restore-replay':
        rows=db.execute("SELECT id FROM plans WHERE state IN ('pending','applied') ORDER BY created").fetchall()
        for row in rows: ledger.apply(row['id'],restore=True)
        if ledger.services.get('product'):
            # A backup cannot prove that an old opt-in is still current.
            ledger.run_sql('product', '''DO $$ BEGIN IF to_regclass('public.statistics_consents') IS NOT NULL
                THEN DELETE FROM public.statistics_consents; END IF; END $$;''')
        out={'replayed':len(rows),'sharingRequiresFreshConsent':bool(ledger.services.get('product'))}
    else:
        out={tbl:[dict(row) for row in db.execute('SELECT * FROM '+tbl)] for tbl in ('policies','holds','requests')}
    return out

if __name__=='__main__':
    cli()
