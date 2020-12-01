using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Internal.Extensions;
using Xunit;

namespace CliWrap.Tests
{
    public class StreamExtensionsSpecs
    {
        private const string Line1 = nameof(Line1); 
        private const string Line2 = nameof(Line2); 
        private const string Line3 = nameof(Line3); 
        
        [Fact]
        public async Task I_can_read_a_stream_with_line_breaks_after_each_line()
        {
            // Arrange
            string content = $"{Line1}\n{Line2}\n";
            
            // Act
            string[] lines = await ReadLines(content);
            
            // Assert
            Assert.Equal(2, lines.Length);
            Assert.Equal(Line1, lines[0]);
            Assert.Equal(Line2, lines[1]);
        }

        [Fact]
        public async Task I_can_read_a_stream_with_line_breaks_after_each_but_the_last_line()
        {
            // Arrange
            string content = $"{Line1}\n{Line2}";
            
            // Act
            string[] lines = await ReadLines(content);
            
            // Assert
            Assert.Equal(2, lines.Length);
            Assert.Equal(Line1, lines[0]);
            Assert.Equal(Line2, lines[1]);
        }
        
        [Fact]
        public async Task I_can_read_a_stream_with_empty_lines()
        {
            // Arrange
            string content = $"{Line1}\n\n{Line2}";
            
            // Act
            string[] lines = await ReadLines(content);
            
            // Assert
            Assert.Equal(3, lines.Length);
            Assert.Equal(Line1, lines[0]);
            Assert.Equal(string.Empty, lines[1]);
            Assert.Equal(Line2, lines[2]);
        }
        
        [Fact]
        public async Task I_can_read_a_stream_with_windows_style_line_breaks()
        {
            // Arrange
            string content = $"{Line1}\r\n{Line2}\r\n{Line3}";
            
            // Act
            string[] lines = await ReadLines(content);
            
            // Assert
            Assert.Equal(3, lines.Length);
            Assert.Equal(Line1, lines[0]);
            Assert.Equal(Line2, lines[1]);
            Assert.Equal(Line3, lines[2]);
        }
        
        [Fact]
        public async Task I_can_read_a_stream_with_windows_style_line_breaks_and_empty_lines()
        {
            // Arrange
            string content = $"{Line1}\r\n\r\n{Line3}";
            
            // Act
            string[] lines = await ReadLines(content);
            
            // Assert
            Assert.Equal(3, lines.Length);
            Assert.Equal(Line1, lines[0]);
            Assert.Equal(string.Empty, lines[1]);
            Assert.Equal(Line3, lines[2]);
        }
        
        [Fact]
        public async Task I_can_read_a_stream_with_carriage_returns_at_the_start_of_each_line()
        {
            // Arrange
            string content = $"\r{Line1}\r{Line2}\n";
            
            // Act
            string[] lines = await ReadLines(content);
            
            // Assert
            Assert.Equal(2, lines.Length);
            Assert.Equal(Line1, lines[0]);
            Assert.Equal(Line2, lines[1]);
        }
        
        [Fact]
        public async Task I_can_read_a_stream_with_mixed_line_breaks_carriage_returns()
        {
            // Arrange
            const string lineStart = "Download started";
            const string progress1 = "Progress: 25%";
            const string progress2 = "Progress: 50%";
            const string progress3 = "Progress: 75%";
            const string lineEnd = "Download finished";
            string content = $"{lineStart}\n" +
                             $"\r{progress1}" +
                             $"\r{progress2}" +
                             $"\r{progress3}\n" +
                             $"{lineEnd}\n";
            
            // Act
            string[] lines = await ReadLines(content);
            
            // Assert
            Assert.Equal(5, lines.Length);
            Assert.Equal(lineStart, lines[0]);
            Assert.Equal(progress1, lines[1]);
            Assert.Equal(progress2, lines[2]);
            Assert.Equal(progress3, lines[3]);
            Assert.Equal(lineEnd, lines[4]);
        }
        
        private static async Task<string[]> ReadLines(string content)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(buffer);
            var reader = new StreamReader(stream);
            
            var lines = new List<string>();
            await foreach (string line in reader.ReadAllLinesAsync(CancellationToken.None))
            {
                lines.Add(line);
            }

            return lines.ToArray();
        }
    }
}