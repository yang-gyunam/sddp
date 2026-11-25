using System.IO.Pipelines;
using Microsoft.AspNetCore.Http.Features;

namespace Sddp.Api.Security;

// Count application body bytes without buffering the response or changing its flush behavior.
internal sealed class CountingResponseBody(IHttpResponseBodyFeature inner) : IHttpResponseBodyFeature
{
    private CountingStream? _stream;
    private CountingWriter? _writer;
    private long _bytes;

    internal long Bytes => Interlocked.Read(ref _bytes);
    public Stream Stream => _stream ??= new CountingStream(inner.Stream, Add);
    public PipeWriter Writer => _writer ??= new CountingWriter(inner.Writer, Add);

    private void Add(long bytes) => Interlocked.Add(ref _bytes, bytes);
    public void DisableBuffering() => inner.DisableBuffering();
    public Task StartAsync(CancellationToken cancellationToken = default) => inner.StartAsync(cancellationToken);
    public Task CompleteAsync() => inner.CompleteAsync();

    public async Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default)
    {
        var length = count ?? Math.Max(0, new FileInfo(path).Length - offset);
        await inner.SendFileAsync(path, offset, count, cancellationToken);
        Add(length);
    }

    private sealed class CountingWriter(PipeWriter writer, Action<long> add) : PipeWriter
    {
        public override bool CanGetUnflushedBytes => writer.CanGetUnflushedBytes;
        public override long UnflushedBytes => writer.UnflushedBytes;
        public override Memory<byte> GetMemory(int sizeHint = 0) => writer.GetMemory(sizeHint);
        public override Span<byte> GetSpan(int sizeHint = 0) => writer.GetSpan(sizeHint);
        public override void Advance(int bytes)
        {
            writer.Advance(bytes);
            add(bytes);
        }

        public override void CancelPendingFlush() => writer.CancelPendingFlush();
        public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default) => writer.FlushAsync(cancellationToken);
        public override void Complete(Exception? exception = null) => writer.Complete(exception);
        public override ValueTask CompleteAsync(Exception? exception = null) => writer.CompleteAsync(exception);
    }

    private sealed class CountingStream(Stream stream, Action<long> add) : Stream
    {
        public override bool CanRead => stream.CanRead;
        public override bool CanSeek => stream.CanSeek;
        public override bool CanWrite => stream.CanWrite;
        public override long Length => stream.Length;
        public override long Position { get => stream.Position; set => stream.Position = value; }
        public override void Flush() => stream.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => stream.FlushAsync(cancellationToken);
        public override int Read(byte[] buffer, int offset, int count) => stream.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => stream.Seek(offset, origin);
        public override void SetLength(long value) => stream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            stream.Write(buffer);
            add(buffer.Length);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await stream.WriteAsync(buffer, cancellationToken);
            add(buffer.Length);
        }
    }
}
