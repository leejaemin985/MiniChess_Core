using System;
using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Common;
using MiniChess.Core.Skills.Areas;
using MiniChess.Core.State;
using MiniChess.Core.Tests.Support;
using Xunit;

namespace MiniChess.Core.Tests
{
    public class AreaTests
    {
        private static readonly string[] ForwardLine = { "#", "#", "o" };

        private static (GameState State, Unit Caster) Setup(int x = 3, int y = 3)
        {
            var (state, u) = new TestGame().Place(Team.Player1, x, y).Start();
            return (state, u[0]);
        }

        private static HashSet<Position> Cells(params (int X, int Y)[] cells)
        {
            return cells.Select(c => new Position(c.X, c.Y)).ToHashSet();
        }

        #region AreaShape

        [Fact]
        public void Parse_FirstRowIsUp_AndExcludedAnchorIsNotACell()
        {
            AreaShape shape = AreaShape.Parse(ForwardLine);

            Assert.Equal(new[] { (0, 2), (0, 1) }, shape.Offsets);
        }

        [Fact]
        public void Parse_IncludedAnchorIsACell()
        {
            AreaShape shape = AreaShape.Parse(new[] { "##", "@#" });

            Assert.Equal(new[] { (0, 1), (1, 1), (0, 0), (1, 0) }, shape.Offsets);
        }

        [Fact]
        public void Parse_NullIsUnset()
        {
            Assert.Null(AreaShape.Parse(null));
        }

        [Theory]
        [InlineData("###")]      // 기준점 없음
        [InlineData("@#@")]      // 기준점 둘
        [InlineData("#@|#")]     // 줄 길이 불일치
        [InlineData("#@x")]      // 알 수 없는 문자
        public void Parse_InvalidGrid_Throws(string rows)
        {
            Assert.Throws<ArgumentException>(() => AreaShape.Parse(rows.Split('|')));
        }

        #endregion

        #region PatternArea

        [Fact]
        public void TargetAnchor_SpreadsFromSelectedCell_AndClipsToBoard()
        {
            var (state, caster) = Setup();
            var area = new PatternArea(AreaShape.Parse(new[] { "###", "#@#", "###" }), AreaAnchor.Target);

            IReadOnlyList<Position> cells = area.GetCells(state, caster, new Position(0, 0));

            Assert.Equal(Cells((0, 1), (1, 1), (0, 0), (1, 0)), cells.ToHashSet());
        }

        [Fact]
        public void CasterAnchor_SpreadsFromCaster_IgnoringSelectedCell()
        {
            var (state, caster) = Setup();
            var area = new PatternArea(AreaShape.Parse(new[] { "#o#" }), AreaAnchor.Caster);

            IReadOnlyList<Position> cells = area.GetCells(state, caster, new Position(0, 0));

            Assert.Equal(Cells((2, 3), (4, 3)), cells.ToHashSet());
        }

        [Theory]
        [InlineData(AreaRotation.R0, new[] { 2, 4, 3, 4, 4, 4, 5, 4 })]     // 그린 그대로: 가로, 오른쪽으로 2칸
        [InlineData(AreaRotation.R90, new[] { 3, 5, 3, 4, 3, 3, 3, 2 })]    // 시계 90: 세로, 아래로 2칸
        [InlineData(AreaRotation.R180, new[] { 4, 4, 3, 4, 2, 4, 1, 4 })]   // 가로, 왼쪽으로 2칸
        [InlineData(AreaRotation.R270, new[] { 3, 3, 3, 4, 3, 5, 3, 6 })]   // 세로, 위로 2칸
        public void TargetAnchor_Rotate_TurnsClockwiseAroundSelectedCell_RegardlessOfCasterDirection(AreaRotation rotation, int[] expected)
        {
            var (state, caster) = Setup();
            var area = new PatternArea(AreaShape.Parse(new[] { "#@##" }), AreaAnchor.Target, rotate: true);

            // 시전자 (3,3) 기준 대각선 칸도 기준점이 될 수 있다
            IReadOnlyList<Position> cells = area.GetCells(state, caster, new Position(3, 4), rotation);

            var expectedCells = Enumerable.Range(0, expected.Length / 2).Select(i => new Position(expected[i * 2], expected[i * 2 + 1]));
            Assert.Equal(expectedCells.ToHashSet(), cells.ToHashSet());
        }

        [Theory]
        [InlineData(AreaRotation.R0, new[] { 3, 4, 3, 5 })]    // 위
        [InlineData(AreaRotation.R90, new[] { 4, 3, 5, 3 })]   // 오른쪽
        [InlineData(AreaRotation.R180, new[] { 3, 2, 3, 1 })]  // 아래
        [InlineData(AreaRotation.R270, new[] { 2, 3, 1, 3 })]  // 왼쪽
        public void CasterAnchor_Rotate_TurnsClockwiseAroundCaster(AreaRotation rotation, int[] expected)
        {
            var (state, caster) = Setup();
            var area = new PatternArea(AreaShape.Parse(ForwardLine), AreaAnchor.Caster, rotate: true);

            IReadOnlyList<Position> cells = area.GetCells(state, caster, caster.Position.Value, rotation);

            Assert.Equal(Cells((expected[0], expected[1]), (expected[2], expected[3])), cells.ToHashSet());
        }

        [Fact]
        public void NonRotatingArea_IgnoresRotation()
        {
            var (state, caster) = Setup();
            var area = new PatternArea(AreaShape.Parse(new[] { "#@##" }), AreaAnchor.Target);

            IReadOnlyList<Position> cells = area.GetCells(state, caster, new Position(3, 3), AreaRotation.R90);

            Assert.Equal(Cells((2, 3), (3, 3), (4, 3), (5, 3)), cells.ToHashSet());
        }

        [Theory]
        [InlineData(AreaRotation.R0, AreaRotation.R90)]
        [InlineData(AreaRotation.R90, AreaRotation.R180)]
        [InlineData(AreaRotation.R180, AreaRotation.R270)]
        [InlineData(AreaRotation.R270, AreaRotation.R0)]
        public void Rotation_Next_StepsClockwiseAndWraps(AreaRotation current, AreaRotation next)
        {
            Assert.Equal(next, current.Next());
        }

        [Fact]
        public void UnsetShape_IsReportedAsConfigIssue()
        {
            var issues = new List<string>();
            new PatternArea(null, AreaAnchor.Target).CollectConfigIssues("SKILL.Area", issues);

            Assert.Contains("SKILL.Area.Shape 미설정", issues);
        }

        #endregion
    }
}
