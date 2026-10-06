using System;
using System.Collections.Generic;
using System.IO;
using Fogline.Sim;
using UnityEngine;
namespace Fogline.Game
{

    /// <summary>
    /// 试玩日志：按步记录玩家指令，每行一条 JSON（步号、指令类型、指令字段），可原样回放。
    /// </summary>
    public sealed class CommandLog
    {
        [Serializable]
        private struct Line
        {
            public long   Step;
            public string Type;
            public string Json;
        }

        private readonly List<string> _lines = new();
        public IReadOnlyList<string> Lines => _lines;

        public void Record(long step, ISimCommand command) =>
            _lines.Add(JsonUtility.ToJson(new Line
            {
                Step = step,
                Type = command.GetType().FullName,
                Json = JsonUtility.ToJson(command),
            }));

        public void WriteTo(string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllLines(path, _lines);
        }

        public static List<(long Step, ISimCommand Command)> Parse(IEnumerable<string> lines)
        {
            var result = new List<(long, ISimCommand)>();
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = JsonUtility.FromJson<Line>(raw);
                var type = typeof(ISimCommand).Assembly.GetType(line.Type);
                if (type == null || !typeof(ISimCommand).IsAssignableFrom(type))
                    throw new FormatException($"不是已知的指令类型：{line.Type}");
                result.Add((line.Step, (ISimCommand)JsonUtility.FromJson(line.Json, type)));
            }
            return result;
        }
    }
}
