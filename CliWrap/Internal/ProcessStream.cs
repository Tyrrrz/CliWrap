using System.Diagnostics;
using System.IO;

namespace CliWrap.Internal
{
    public class ProcessStream : Stream
    {
        private readonly Cli _cli;
        private Process? _process;

        public ProcessStream(Cli cli)
        {
            _cli = cli;
        }

        private Stream GetInnerStream() => (_process ??= _cli.Start()).StandardOutput.BaseStream;

        public override void Flush() => GetInnerStream().Flush();

        public override int Read(byte[] buffer, int offset, int count) => GetInnerStream().Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => GetInnerStream().Seek(offset, origin);

        public override void SetLength(long value) => GetInnerStream().SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => GetInnerStream().Write(buffer, offset, count);

        public override bool CanRead => GetInnerStream().CanRead;
        public override bool CanSeek => GetInnerStream().CanSeek;
        public override bool CanWrite => GetInnerStream().CanWrite;
        public override long Length => GetInnerStream().Length;
        public override long Position
        {
            get => GetInnerStream().Position;
            set => GetInnerStream().Position = value;
        }
    }
}