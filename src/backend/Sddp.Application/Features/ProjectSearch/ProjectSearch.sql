WITH conversations_in_scope AS NOT MATERIALIZED (
    SELECT c.* FROM conversations c
    WHERE @Conversations AND c.tenant_id=@Tenant AND c.project_id=@Project AND c.is_active
      AND ((c.visibility='Public' AND c.conversation_type <> 'DirectMessage') OR EXISTS
          (SELECT 1 FROM conversation_members m WHERE m.conversation_id=c.id AND m.user_id=@User AND m.is_active))
), files_in_scope AS NOT MATERIALIZED (
    SELECT f.* FROM project_search_files f JOIN project_search_file_heads h USING (tenant_id,project_id)
    WHERE f.tenant_id=@Tenant AND f.project_id=@Project AND h.revision=@GitCommit
), entities(menu,key,target,kind,title,code,body,location,status,version,updated_at,revision,entity_type,entity_id) AS NOT MATERIALIZED (
    SELECT 'specs',s.id::text,s.id::text,'spec',s.title,s.code,
        coalesce(s.description,'') || ' ' || coalesce(s.decision,'') || ' ' || coalesce(s.context,'') || ' ' || coalesce(s.scope,'') || ' ' || coalesce(s.out_of_scope,'') || ' ' || coalesce(s.definitions,'') || ' ' || coalesce(s.acceptance_criteria,''),
        '',c.name,s.version,s.updated_at,NULL::text,0,s.id
    FROM specs s JOIN codes c ON c.id=s.status_code_id WHERE @Specs AND s.tenant_id=@Tenant AND s.project_id=@Project AND s.is_active AND s.valid_to IS NULL
    UNION ALL
    SELECT 'requirements',r.id::text,r.id::text,'requirement',r.title,r.code,coalesce(r.description,'') || ' ' || coalesce(r.rationale,'') || ' ' || coalesce(r.source,''),
        '',c.name,r.version,r.updated_at,NULL,1,r.id
    FROM requirements r JOIN codes c ON c.id=r.status_code_id WHERE @Requirements AND r.tenant_id=@Tenant AND r.project_id=@Project AND r.is_active AND r.valid_to IS NULL
    UNION ALL
    SELECT 'glossary',g.id::text,g.id::text,'glossary',g.term,coalesce(g.abbreviation,''),concat_ws(' ',g.definition,g.synonyms,g.usage_examples::text),coalesce(g.synonyms,''),c.name,g.version,g.updated_at,NULL,3,g.id
    FROM glossary_terms g JOIN codes c ON c.id=g.status_code_id WHERE @Glossary AND g.tenant_id=@Tenant AND g.project_id=@Project AND g.is_active AND g.valid_to IS NULL
    UNION ALL
    SELECT 'conversations',c.id::text,c.id::text,CASE WHEN c.conversation_type='Forum' THEN 'forum' ELSE 'conversation' END,c.name,'',coalesce(c.description,''),c.name,c.conversation_type,'',c.updated_at,NULL,4,c.id
    FROM conversations_in_scope c
    UNION ALL
    SELECT 'conversations',t.id::text,c.id::text,'topic',t.title,'',t.title,c.name,cd.name,'',t.updated_at,t.id::text,2,t.id
    FROM topics t JOIN conversations_in_scope c ON c.id=t.forum_id JOIN codes cd ON cd.id=t.status_code_id WHERE t.is_active
    UNION ALL
    SELECT 'tasks',t.id::text,t.id::text,'task',t.title,'',t.description,coalesce(u.display_name,''),c.name,'',t.updated_at,NULL,6,t.id
    FROM tasks t JOIN codes c ON c.id=t.status_code_id LEFT JOIN users u ON u.id=t.assignee_id WHERE @Tasks AND t.tenant_id=@Tenant AND t.project_id=@Project AND t.is_active
    UNION ALL
    SELECT 'diagrams',d.id::text,d.id::text,'diagram',d.title,'',
        (SELECT coalesce(string_agg(segment#>>'{}',' '),'') FROM jsonb_path_query(d.model_json,'lax $.**.keyvalue() ? (@.key == "label" || @.key == "name" || @.key == "text" || @.key == "description" || @.key == "title").value', '{}', true) segment),
        d.kind,d.kind,d.revision::text,d.updated_at,NULL,7,d.id
    FROM diagrams d WHERE d.tenant_id=@Tenant AND d.project_id=@Project AND d.is_active AND d.id=ANY(@Diagrams)
    UNION ALL
    SELECT 'artifacts',a.id::text,a.id::text,'artifact',coalesce(nullif(a.entity_name,''),a.artifact_path),'',concat_ws(' ',a.entity_name,a.db_object_name,a.artifact_type,f.content),
        a.artifact_path,a.artifact_category,a.generator_version,a.updated_at,NULL,5,a.id
    FROM artifact_trackings a JOIN projects p ON p.id=a.project_id AND p.tenant_id=a.tenant_id
    LEFT JOIN files_in_scope f ON f.path=concat_ws('/',nullif(nullif(trim(both '/' from p.artifact_root_path),'.'),''),ltrim(a.artifact_path,'/'))
    WHERE a.tenant_id=@Tenant AND a.project_id=@Project AND a.is_active
), ownership(name,owner_id,updated_at) AS NOT MATERIALIZED (
    SELECT coalesce(nullif(s.code,''),s.title), (SELECT u.id FROM unnest(string_to_array(s.owners,',')) WITH ORDINALITY o(value,ordinal)
        JOIN users u ON u.id::text=lower(trim(o.value)) ORDER BY ordinal LIMIT 1), s.updated_at
    FROM specs s WHERE @Specs AND s.tenant_id=@Tenant AND s.project_id=@Project AND s.is_active AND s.valid_to IS NULL
    UNION ALL SELECT coalesce(nullif(code,''),title),owner_user_id,updated_at FROM requirements
        WHERE @Requirements AND tenant_id=@Tenant AND project_id=@Project AND is_active AND valid_to IS NULL
    UNION ALL SELECT term,owner_user_id,updated_at FROM glossary_terms
        WHERE @Glossary AND tenant_id=@Tenant AND project_id=@Project AND is_active AND valid_to IS NULL
    UNION ALL SELECT title,coalesce(assignee_id,creator_id),updated_at FROM tasks
        WHERE @Tasks AND tenant_id=@Tenant AND project_id=@Project AND is_active
    UNION ALL SELECT artifact_path,owner_user_id,updated_at FROM artifact_trackings
        WHERE tenant_id=@Tenant AND project_id=@Project AND is_active
), pages AS NOT MATERIALIZED (
    SELECT * FROM knowledge_pages p WHERE @Knowledge AND p.tenant_id=@Tenant AND p.project_id=@Project AND p.is_active
      AND (p.status='Published' OR (p.status='Draft' AND (@AllDrafts OR p.created_by=@User)) OR (p.status='Archived' AND @Archived))
), documents(menu,key,target,kind,title,code,body,location,status,version,updated_at,revision) AS NOT MATERIALIZED (
    SELECT menu,key,target,kind,title,code,body,location,status,version,updated_at,revision FROM entities
    UNION ALL
    SELECT 'knowledge',p.id::text,p.id::text,'knowledge',p.title,p.slug,p.content_text,p.tags_json::text,p.status,p.revision::text,p.updated_at,NULL FROM pages p
    UNION ALL
    SELECT 'knowledge',v.id::text,p.id::text,'knowledge-version',v.title,v.slug,v.content_text,v.tags_json::text,v.status,v.revision::text,v.updated_at,v.revision::text
    FROM pages p JOIN knowledge_page_versions v ON v.tenant_id=p.tenant_id AND v.project_id=p.project_id AND v.page_id=p.id
    WHERE v.is_active AND v.revision<>p.revision AND (v.status='Published' OR (v.status='Draft' AND (@AllDrafts OR p.created_by=@User)) OR (v.status='Archived' AND @Archived))
    UNION ALL
    SELECT 'conversations',m.id::text,c.id::text,CASE WHEN t.id IS NULL THEN 'message' ELSE 'topic-message' END,
        coalesce(t.title,c.name),'',m.content,concat_ws(' › ',c.name,t.title,u.display_name),cd.name,'',m.updated_at,t.id::text
    FROM messages m LEFT JOIN topics t ON t.id=m.topic_id AND t.is_active JOIN conversations_in_scope c ON c.id=coalesce(m.conversation_id,t.forum_id)
    JOIN codes cd ON cd.id=m.type_code_id LEFT JOIN users u ON u.id=m.sender_id
    WHERE m.tenant_id=@Tenant AND m.project_id=@Project AND m.is_active
      AND (@IncludeAiConversationMessages OR (m.topic_id IS NULL AND c.conversation_type <> 'Channel')
           OR (u.is_ai IS FALSE AND m.type_code_id <> ALL(@AiMessageTypes)))
    UNION ALL
    SELECT 'files',f.path,f.path,'file',regexp_replace(f.path,'^.*/',''),'',f.content,f.path,'',left(@GitCommit,10),f.updated_at,@GitCommit FROM files_in_scope f
    UNION ALL
    SELECT 'tasks',ac.id::text,t.id::text,'criterion',t.title,'',ac.description,'Acceptance criteria',
        CASE WHEN ac.completed THEN 'Done' ELSE 'Open' END,'',ac.updated_at,NULL
    FROM task_acceptance_criteria ac JOIN tasks t ON t.id=ac.task_id
    WHERE @Tasks AND t.tenant_id=@Tenant AND t.project_id=@Project AND t.is_active
    UNION ALL
    SELECT 'tasks',log.id::text,t.id::text,'task-time-log',t.title,'',log.description,concat_ws(' › ',u.display_name,log.log_date::text),
        log.hours::text || ' h','',log.created_at,NULL
    FROM task_time_logs log JOIN tasks t ON t.id=log.task_id JOIN users u ON u.id=log.user_id
    WHERE @Tasks AND t.tenant_id=@Tenant AND t.project_id=@Project AND t.is_active
    UNION ALL
    SELECT 'effort',w.id::text,w.user_id::text,'worklog',u.display_name,'',concat_ws(' ',w.note,t.title),w.work_date::text,w.spent_hours::text || ' h','',w.updated_at,w.work_date::text
    FROM worklogs w JOIN users u ON u.id=w.user_id LEFT JOIN tasks t ON @Tasks AND t.id=w.task_id AND t.tenant_id=w.tenant_id AND t.project_id=w.project_id AND t.is_active
    WHERE w.tenant_id=@Tenant AND w.project_id=@Project AND w.is_active
    UNION ALL
    SELECT 'effort',a.id::text,a.user_id::text,'allocation',u.display_name,'','Allocation 배정',a.allocation_date::text,a.allocated_hours::text || ' h','',a.updated_at,a.allocation_date::text
    FROM effort_allocations a JOIN users u ON u.id=a.user_id WHERE a.tenant_id=@Tenant AND a.project_id=@Project AND a.is_active
    UNION ALL
    SELECT 'dashboard','stats-'||e.menu,@Project::text,'statistic',e.menu,'',e.menu || ' ' || count(*)::text,
        'Project statistics','', '',max(e.updated_at),NULL
    FROM entities e WHERE e.entity_type NOT IN (2,7) GROUP BY e.menu
    UNION ALL
    SELECT 'dashboard','owner-'||coalesce(o.owner_id::text,'unassigned'),coalesce(o.owner_id::text,'unassigned'),'ownership',
        coalesce(u.display_name,'Unassigned'),'',
        string_agg(o.name,' · ' ORDER BY o.name),'Ownership Map','', '',max(o.updated_at),NULL
    FROM ownership o LEFT JOIN users u ON u.id=o.owner_id GROUP BY o.owner_id,u.display_name
    UNION ALL
    SELECT 'dashboard','stats-effort',@Project::text,'statistic','Effort','','Effort ' || trunc(sum(w.spent_hours))::text || ' h',
        'Project statistics','','',max(w.updated_at),NULL
    FROM worklogs w WHERE w.tenant_id=@Tenant AND w.project_id=@Project AND w.is_active AND w.work_date >= now() - interval '7 days'
    HAVING count(*)>0
    UNION ALL
    SELECT 'dashboard',p.id::text,p.id::text,'project',p.name,p.code,coalesce(p.description,''),'Overview',p.status,'',p.updated_at,NULL FROM projects p WHERE p.tenant_id=@Tenant AND p.id=@Project AND p.is_active
    UNION ALL
    SELECT 'dashboard',m.id::text,m.user_id::text,'member',u.display_name,'',u.username,'Project members',r.name,'',m.updated_at,NULL
    FROM project_members m JOIN projects p ON p.id=m.project_id JOIN users u ON u.id=m.user_id AND u.is_active JOIN user_roles ur ON ur.id=m.user_role_id AND ur.is_active JOIN roles r ON r.id=ur.role_id AND r.is_active
    WHERE p.tenant_id=@Tenant AND p.id=@Project AND m.is_active
    UNION ALL
    SELECT 'dashboard',a.id::text,a.id::text,'activity',a.action,'',coalesce(a.description,''),a.resource_type,'','',a.created_at,NULL
    FROM audit_logs a WHERE @Audit AND a.tenant_id=@Tenant AND a.project_id=@Project AND a.actor_id=@User AND a.is_active
    UNION ALL
    SELECT 'traceability',r.id::text,l.entity_id::text,'relationship',l.title || ' → ' || h.title,concat_ws(' ',l.code,h.code),concat_ws(' ',r.reason,c.name,l.title,h.title),c.name,'','',r.updated_at,h.entity_id::text
    FROM relationships r JOIN entities l ON l.entity_type=r.from_entity_type AND l.entity_id=r.from_entity_id
    JOIN entities h ON h.entity_type=r.to_entity_type AND h.entity_id=r.to_entity_id JOIN codes c ON c.id=r.type_code_id
    WHERE r.tenant_id=@Tenant AND r.project_id=@Project AND r.is_active AND r.valid_to IS NULL
    UNION ALL
    SELECT 'ai-chat',chat.key,chat.key,'ai-chat',chat.title,'',chat.body,chat.location,'','',chat."updatedAt",NULL
    FROM jsonb_to_recordset(CAST(@Chats AS jsonb)) AS chat(key text,title text,body text,location text,"updatedAt" timestamptz)
)
