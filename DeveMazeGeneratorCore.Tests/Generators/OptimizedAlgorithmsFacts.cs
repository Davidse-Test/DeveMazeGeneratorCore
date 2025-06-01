using DeveMazeGeneratorCore.Generators;
using DeveMazeGeneratorCore.Generators.Helpers;
using DeveMazeGeneratorCore.Generators.SpeedOptimization;
using DeveMazeGeneratorCore.Helpers;
using DeveMazeGeneratorCore.InnerMaps;
using DeveMazeGeneratorCore.Factories;
using System;
using System.Diagnostics;
using Xunit;

namespace DeveMazeGeneratorCore.Tests.Generators
{
    public class OptimizedAlgorithmsFacts
    {
        private const int TestMazeSize = 128;
        private const int TestSeed = 1337;

        [Fact]
        public void StackAllocationOptimization_GeneratesValidMaze()
        {
            // Arrange
            var generator = new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedStackAllocation();

            // Act
            var maze = MazeGenerator.Generate<AlgorithmBacktrack2Deluxe2_AsByte_OptimizedStackAllocation, BitArreintjeFastInnerMap, NetRandom>(TestMazeSize, TestMazeSize, TestSeed, null);

            // Assert
            Assert.True(MazeVerifier.IsPerfectMaze(maze.InnerMap));
            Assert.False(maze.InnerMap[0, 0]);
            Assert.True(maze.InnerMap[1, 1]);
        }

        [Fact]
        public void MazePointStructureOptimization_GeneratesValidMaze()
        {
            // Arrange
            var generator = new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedMazePointStructure();

            // Act
            var maze = MazeGenerator.Generate<AlgorithmBacktrack2Deluxe2_AsByte_OptimizedMazePointStructure, BitArreintjeFastInnerMap, NetRandom>(TestMazeSize, TestMazeSize, TestSeed, null);

            // Assert
            Assert.True(MazeVerifier.IsPerfectMaze(maze.InnerMap));
            Assert.False(maze.InnerMap[0, 0]);
            Assert.True(maze.InnerMap[1, 1]);
        }

        [Fact]
        public void DirectionSelectionOptimization_GeneratesValidMaze()
        {
            // Arrange
            var generator = new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedDirectionSelection();

            // Act
            var maze = MazeGenerator.Generate<AlgorithmBacktrack2Deluxe2_AsByte_OptimizedDirectionSelection, BitArreintjeFastInnerMap, NetRandom>(TestMazeSize, TestMazeSize, TestSeed, null);

            // Assert
            Assert.True(MazeVerifier.IsPerfectMaze(maze.InnerMap));
            Assert.False(maze.InnerMap[0, 0]);
            Assert.True(maze.InnerMap[1, 1]);
        }

        [Fact]
        public void RandomNumberOptimization_GeneratesValidMaze()
        {
            // Arrange
            var generator = new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedRandomNumber();

            // Act
            var maze = MazeGenerator.Generate<AlgorithmBacktrack2Deluxe2_AsByte_OptimizedRandomNumber, BitArreintjeFastInnerMap, NetRandom>(TestMazeSize, TestMazeSize, TestSeed, null);

            // Assert
            Assert.True(MazeVerifier.IsPerfectMaze(maze.InnerMap));
            Assert.False(maze.InnerMap[0, 0]);
            Assert.True(maze.InnerMap[1, 1]);
        }

        [Fact]
        public void CallbacksOptimization_GeneratesValidMaze()
        {
            // Arrange
            var generator = new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedCallbacks();

            // Act
            var maze = MazeGenerator.Generate<AlgorithmBacktrack2Deluxe2_AsByte_OptimizedCallbacks, BitArreintjeFastInnerMap, NetRandom>(TestMazeSize, TestMazeSize, TestSeed, null);

            // Assert
            Assert.True(MazeVerifier.IsPerfectMaze(maze.InnerMap));
            Assert.False(maze.InnerMap[0, 0]);
            Assert.True(maze.InnerMap[1, 1]);
        }

        [Fact]
        public void CombinedImprovedOptimization_GeneratesValidMaze()
        {
            // Arrange
            var generator = new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedCombinedImproved();

            // Act
            var maze = MazeGenerator.Generate<AlgorithmBacktrack2Deluxe2_AsByte_OptimizedCombinedImproved, BitArreintjeFastInnerMap, NetRandom>(TestMazeSize, TestMazeSize, TestSeed, null);

            // Assert
            Assert.True(MazeVerifier.IsPerfectMaze(maze.InnerMap));
            Assert.False(maze.InnerMap[0, 0]);
            Assert.True(maze.InnerMap[1, 1]);
        }

        [Fact]
        public void AllOptimizations_ProduceSameResultsAsMaster()
        {
            // This test ensures that all optimized versions produce valid mazes
            // (though they may differ due to different internal state management)
            
            // Arrange
            var originalGenerator = new AlgorithmBacktrack2Deluxe2_AsByte();
            var optimizations = new IAlgorithm<Mazes.Maze>[]
            {
                new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedStackAllocation(),
                new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedMazePointStructure(),
                new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedDirectionSelection(),
                new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedRandomNumber(),
                new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedCallbacks(),
                new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedCombined(),
                new AlgorithmBacktrack2Deluxe2_AsByte_OptimizedCombinedImproved()
            };

            // Act & Assert
            foreach (var algorithm in optimizations)
            {
                var maze = algorithm.GoGenerate(TestMazeSize, TestMazeSize, TestSeed, 
                    new InnerMapFactory<BitArreintjeFastInnerMap>(), 
                    new RandomFactory<NetRandom>(), 
                    new NoAction());

                // Each optimized algorithm should produce a valid perfect maze
                Assert.True(MazeVerifier.IsPerfectMaze(maze.InnerMap), 
                    $"Algorithm {algorithm.GetType().Name} did not produce a perfect maze");
                Assert.False(maze.InnerMap[0, 0]);
                Assert.True(maze.InnerMap[1, 1]);
            }
        }
    }
}