using System;
using System.IO;
using Fogline.Game;
using Fogline.Sim;
using NUnit.Framework;

namespace Fogline.Tests
{
    public class CommandLogTests
    {
        [Test]
        public void Parse_RoundTripsStepTypeAndFields()
        {
            var log = new CommandLog();
            log.Record(7, new ShiftController(-1));
            var parsed = CommandLog.Parse(log.Lines);
            Assert.AreEqual(1, parsed.Count);
            Assert.AreEqual(7, parsed[0].Step);
            Assert.AreEqual(-1, ((ShiftController)parsed[0].Command).Delta);
        }

        [Test]
        public void Parse_RestoreMarkerDropsAbandonedTimeline()
        {
            var log = new CommandLog();
            log.Record(1, new ShiftController(1));
            log.Record(5, new ShiftController(1));
            log.Record(9, new ShiftController(1));
            log.RecordRestore(5);
            log.Record(6, new ShiftController(1));
            var parsed = CommandLog.Parse(log.Lines);
            Assert.AreEqual(2, parsed.Count);
            Assert.AreEqual(1, parsed[0].Step);
            Assert.AreEqual(6, parsed[1].Step);
        }

        [Test]
        public void Parse_MalformedJsonThrowsFormatException()
        {
            Assert.Throws<FormatException>(() => CommandLog.Parse(new[] { "not json {" }));
        }

        [Test]
        public void Parse_SkipsBlankLines()
        {
            var log = new CommandLog();
            log.Record(1, new ShiftController(1));
            var lines = new[] { "", log.Lines[0], "   " };
            Assert.AreEqual(1, CommandLog.Parse(lines).Count);
        }

        [Test]
        public void Parse_UnknownTypeThrowsFormatException()
        {
            var line = "{\"Step\":0,\"Type\":\"Fogline.Sim.NoSuchCommand\",\"Json\":\"{}\"}";
            Assert.Throws<FormatException>(() => CommandLog.Parse(new[] { line }));
        }

        [Test]
        public void Parse_NonCommandTypeThrowsFormatException()
        {
            var line = "{\"Step\":0,\"Type\":\"Fogline.Sim.SimClock\",\"Json\":\"{}\"}";
            Assert.Throws<FormatException>(() => CommandLog.Parse(new[] { line }));
        }

        [Test]
        public void WriteTo_CreatesDirectoryAndFileReadsBack()
        {
            var dir = Path.Combine(Path.GetTempPath(), "fogline-log-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(dir, "nested", "run.jsonl");
            try
            {
                var log = new CommandLog();
                log.Record(3, new ShiftController(1));
                log.WriteTo(path);
                Assert.AreEqual(3, CommandLog.Parse(File.ReadAllLines(path))[0].Step);
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }
    }
}
