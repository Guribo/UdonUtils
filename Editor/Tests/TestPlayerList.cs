using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TLP.UdonUtils.Runtime.Common;
using TLP.UdonUtils.Runtime.Logger;
using TLP.UdonUtils.Runtime.Player;
using TLP.UdonUtils.Tests.Runtime.Utils;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDKBase;
using VRC.Udon;
using Object = UnityEngine.Object;

namespace TLP.UdonUtils.Tests.Editor
{
    [TestFixture(Category = "TLP")]
    public class TestPlayerList : TestWithLogger
    {
        private GameObject _go;
        private PlayerList _playerList;
        private GameObject _betterPlayerAudioGameObject;
        private GameObject _betterPlayerAudioOverrideGameObject;

        private VRCPlayerApi _player1;
        private VRCPlayerApi _player2;
        private VRCPlayerApi _player3;
        private VRCPlayerApi _player4;

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            _player1 = UdonTestEnvironment.CreatePlayer();
            _player2 = UdonTestEnvironment.CreatePlayer();
            _player3 = UdonTestEnvironment.CreatePlayer();
            _player4 = UdonTestEnvironment.CreatePlayer();

            _go = new GameObject();
            _playerList = _go.AddComponent<PlayerList>();
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(_go);
            UdonTestEnvironment.Deconstruct();
            UdonTestEnvironment = null;
        }

        [Test]
        public void AddPlayer()
        {
#if TLP_DEBUG
            LogAssert.Expect(LogType.Error, new Regex(".*Player invalid.*", RegexOptions.Singleline));
            LogAssert.Expect(LogType.Assert, new Regex(".*Player invalid.*", RegexOptions.Singleline));
#endif
            Assert.False(_playerList.AddPlayer(null));
            Assert.True(_playerList.AddPlayer(LocalPlayer));
            Assert.AreEqual(1, _playerList.players.Length);

            Assert.True(_playerList.AddPlayer(_player1));
            Assert.AreEqual(2, _playerList.players.Length);
            Assert.True(_playerList.AddPlayer(_player2));
            Assert.AreEqual(3, _playerList.players.Length);
            Assert.True(_playerList.AddPlayer(_player3));
            Assert.AreEqual(4, _playerList.players.Length);

            // simulate a few players leaving the world
            _playerList.players[1] = -1;
            _playerList.players[2] = -1;

            // simulate another player joining afterwards
            Assert.True(_playerList.AddPlayer(_player4));
            Assert.AreEqual(3, _playerList.players.Length);
            Assert.AreEqual(new[] { LocalPlayer.playerId, _player3.playerId, _player4.playerId }, _playerList.players);

            // try adding same player again
#if TLP_DEBUG
            LogAssert.Expect(LogType.Error, new Regex(".*Player 4 already in list.*", RegexOptions.Singleline));
            LogAssert.Expect(LogType.Assert, new Regex(".*Player 4 already in list.*", RegexOptions.Singleline));
#endif
            Assert.False(_playerList.AddPlayer(_player4));
            Assert.AreEqual(new[] { LocalPlayer.playerId, _player3.playerId, _player4.playerId }, _playerList.players);

            _playerList.players = new[] { -1, -1, -1 };
            Assert.True(_playerList.AddPlayer(_player4));
            Assert.AreEqual(new[] { _player4.playerId }, _playerList.players);
        }

        [Test]
        public void PlayerCount()
        {
            Assert.AreEqual(0, _playerList.DiscardInvalid());
            Assert.True(_playerList.AddPlayer(LocalPlayer));
            Assert.AreEqual(1, _playerList.DiscardInvalid());

            // try adding again
#if TLP_DEBUG
            LogAssert.Expect(LogType.Error, new Regex(".*Player 0 already in list.*", RegexOptions.Singleline));
            LogAssert.Expect(LogType.Assert, new Regex(".*Player 0 already in list.*", RegexOptions.Singleline));
#endif
            Assert.False(_playerList.AddPlayer(LocalPlayer));
            Assert.AreEqual(1, _playerList.DiscardInvalid());

            Assert.True(_playerList.AddPlayer(_player1));
            Assert.AreEqual(2, _playerList.DiscardInvalid());

            Assert.True(_playerList.AddPlayer(_player2));
            Assert.True(_playerList.AddPlayer(_player3));
            Assert.True(_playerList.AddPlayer(_player4));

            // simulate a few players leaving the world
            _playerList.players[1] = -1;
            _playerList.players[2] = -1;

            // simulate a player leaving the zone
            Assert.True(_playerList.RemovePlayer(_player4));
            Assert.AreEqual(2, _playerList.DiscardInvalid());

            // invalid players are removed
            _playerList.players = new[] { 0, 1, 2, -1, 4 };
            Assert.AreEqual(4, _playerList.DiscardInvalid());
            Assert.AreEqual(new[] { 0, 1, 2, 4 }, _playerList.players);
        }

        [Test]
        public void RemovePlayer()
        {
            Assert.False(_playerList.RemovePlayer(null));
            Assert.False(_playerList.RemovePlayer(LocalPlayer));
            Assert.False(_playerList.RemovePlayer(_player1));

            // remove single existing player
            _playerList.players = new[] { 0 };
            Assert.True(_playerList.RemovePlayer(LocalPlayer));
            Assert.AreEqual(new int[0], _playerList.players);

            // try remove player that has not been added
            _playerList.players = new[] { -1 };
            Assert.False(_playerList.RemovePlayer(LocalPlayer));
            Assert.AreEqual(new int[0], _playerList.players);

            // try remove first player
            _playerList.players = new[] { 0, 1 };
            Assert.True(_playerList.RemovePlayer(LocalPlayer));
            Assert.AreEqual(new[] { 1 }, _playerList.players);

            // try remove second player
            _playerList.players = new[] { 0, 1 };
            Assert.True(_playerList.RemovePlayer(_player1));
            Assert.AreEqual(new[] { 0 }, _playerList.players);

            // try remove player in the middle
            _playerList.players = new[] { 0, 1, 2 };
            Assert.True(_playerList.RemovePlayer(_player1));
            Assert.AreEqual(new[] { 0, 2 }, _playerList.players);

            // try remove player not in the list with invalid inside it
            _playerList.players = new[] { 0, -1, 1 };
            Assert.False(_playerList.RemovePlayer(_player2));
            Assert.AreEqual(new[] { 0, 1 }, _playerList.players);

            // try remove first player followed by invalid
            _playerList.players = new[] { 0, -1, 1 };
            Assert.True(_playerList.RemovePlayer(LocalPlayer));
            Assert.AreEqual(new[] { 1 }, _playerList.players);

            // try remove second player lead by invalid
            _playerList.players = new[] { -1, 0, 1 };
            Assert.True(_playerList.RemovePlayer(LocalPlayer));
            Assert.AreEqual(new[] { 1 }, _playerList.players);

            // try remove second player surrounded by invalid
            _playerList.players = new[] { -1, 0, -1, 1 };
            Assert.True(_playerList.RemovePlayer(LocalPlayer));
            Assert.AreEqual(new[] { 1 }, _playerList.players);
        }

        [Test]
        public void Consolidate()
        {
            Assert.AreEqual(0, _playerList.ConsolidatePlayerIds(null));

            _playerList.players = new int[0];
            Assert.AreEqual(0, _playerList.ConsolidatePlayerIds(_playerList.players));
            Assert.AreEqual(0, _playerList.players.Length);

            _playerList.players = new[] { -1 };
            Assert.AreEqual(0, _playerList.ConsolidatePlayerIds(_playerList.players));
            Assert.AreEqual(1, _playerList.players.Length);
            Assert.AreEqual(int.MaxValue, _playerList.players[0]);

            _playerList.players = new[] { _player1.playerId };
            Assert.AreEqual(1, _playerList.ConsolidatePlayerIds(_playerList.players));
            Assert.AreEqual(1, _playerList.players.Length);
            Assert.AreEqual(_player1.playerId, _playerList.players[0]);

            _playerList.players = new[] { _player1.playerId, -1 };
            Assert.AreEqual(1, _playerList.ConsolidatePlayerIds(_playerList.players));
            Assert.AreEqual(2, _playerList.players.Length);
            Assert.AreEqual(_player1.playerId, _playerList.players[0]);
            Assert.AreEqual(int.MaxValue, _playerList.players[1]);

            _playerList.players = new[] { -1, _player1.playerId };
            Assert.AreEqual(1, _playerList.ConsolidatePlayerIds(_playerList.players));
            Assert.AreEqual(2, _playerList.players.Length);
            Assert.AreEqual(_player1.playerId, _playerList.players[0]);
            Assert.AreEqual(int.MaxValue, _playerList.players[1]);

            _playerList.players = new int[] { -1, -1 };
            Assert.AreEqual(0, _playerList.ConsolidatePlayerIds(_playerList.players));
            Assert.AreEqual(2, _playerList.players.Length);
            Assert.AreEqual(int.MaxValue, _playerList.players[0]);
            Assert.AreEqual(int.MaxValue, _playerList.players[1]);

            _playerList.players = new int[] { -1, -1, -1 };
            Assert.AreEqual(0, _playerList.ConsolidatePlayerIds(_playerList.players));
            Assert.AreEqual(3, _playerList.players.Length);
            Assert.AreEqual(int.MaxValue, _playerList.players[0]);
            Assert.AreEqual(int.MaxValue, _playerList.players[1]);
            Assert.AreEqual(int.MaxValue, _playerList.players[2]);

            _playerList.players = new[] { _player1.playerId, -1, _player2.playerId };
            Assert.AreEqual(2, _playerList.ConsolidatePlayerIds(_playerList.players));
            Assert.AreEqual(3, _playerList.players.Length);
            Assert.AreEqual(_player1.playerId, _playerList.players[0]);
            Assert.AreEqual(_player2.playerId, _playerList.players[1]);
            Assert.AreEqual(int.MaxValue, _playerList.players[2]);

            _playerList.players = new[] { -1, -1, _player2.playerId };
            Assert.AreEqual(1, _playerList.ConsolidatePlayerIds(_playerList.players));
            Assert.AreEqual(3, _playerList.players.Length);
            Assert.AreEqual(_player2.playerId, _playerList.players[0]);
            Assert.AreEqual(int.MaxValue, _playerList.players[1]);
            Assert.AreEqual(int.MaxValue, _playerList.players[2]);

            _playerList.players = new[] { -1, _player1.playerId, _player2.playerId };
            Assert.AreEqual(2, _playerList.ConsolidatePlayerIds(_playerList.players));
            Assert.AreEqual(3, _playerList.players.Length);
            Assert.AreEqual(_player1.playerId, _playerList.players[0]);
            Assert.AreEqual(_player2.playerId, _playerList.players[1]);
            Assert.AreEqual(int.MaxValue, _playerList.players[2]);

            _playerList.players = new[] { _player1.playerId, _player2.playerId, _player3.playerId };
            Assert.AreEqual(3, _playerList.ConsolidatePlayerIds(_playerList.players));
            Assert.AreEqual(3, _playerList.players.Length);
            Assert.AreEqual(_player1.playerId, _playerList.players[0]);
            Assert.AreEqual(_player2.playerId, _playerList.players[1]);
            Assert.AreEqual(_player3.playerId, _playerList.players[2]);
        }

        [Test]
        public void Contains()
        {
            Assert.False(_playerList.Contains(null));
            _playerList.AddPlayer(LocalPlayer);
            Assert.True(_playerList.Contains(LocalPlayer));
        }

        [Test]
        public void ResizePlayerArray()
        {
            Assert.False(_playerList.ResizePlayerArray(-1));
            Assert.AreEqual(new int[0], _playerList.players);

            Assert.True(_playerList.ResizePlayerArray(0));
            Assert.AreEqual(new int[0], _playerList.players);

            Assert.True(_playerList.ResizePlayerArray(1));
            Assert.AreEqual(new int[1] { int.MaxValue }, _playerList.players);

            _playerList.players = new[] { 1, 2, 3 };
            Assert.True(_playerList.ResizePlayerArray(2));
            Assert.AreEqual(new[] { 1, 2 }, _playerList.players);

            _playerList.players = new[] { 1, 2, 3 };
            Assert.True(_playerList.ResizePlayerArray(4));
            Assert.AreEqual(new[] { 1, 2, 3, int.MaxValue }, _playerList.players);

            _playerList.players = new[] { 1, 2, 3 };
            Assert.True(_playerList.ResizePlayerArray(3));
            Assert.AreEqual(new[] { 1, 2, 3 }, _playerList.players);
        }

        [Test]
        public void Clear()
        {
            _playerList.players = new[] { 1, 2, 3 };

            _playerList.Clear();
            Assert.NotNull(_playerList.players);
            Assert.AreEqual(0, _playerList.players.Length);
        }

        #region Sort

        [Test]
        public void SortsEmptyArray()
        {
            int[] arr = new int[0];
            _playerList.Sort(arr);
            Assert.AreEqual(0, arr.Length);
        }

        [Test]
        public void SortsSingleElementArray()
        {
            int[] arr = { 5 };
            _playerList.Sort(arr);
            Assert.AreEqual(1, arr.Length);
            Assert.AreEqual(5, arr[0]);
        }

        [Test]
        public void SortsArrayWithDuplicateElements()
        {
            int[] arr = { 3, 5, 2, 7, 5, 3, 8 };
            _playerList.Sort(arr);
            Assert.AreEqual(7, arr.Length);
            Assert.AreEqual(2, arr[0]);
            Assert.AreEqual(3, arr[1]);
            Assert.AreEqual(3, arr[2]);
            Assert.AreEqual(5, arr[3]);
            Assert.AreEqual(5, arr[4]);
            Assert.AreEqual(7, arr[5]);
            Assert.AreEqual(8, arr[6]);
        }

        [Test]
        public void SortsArrayWithNegativeElements()
        {
            int[] arr = { -3, 5, 2, -7, 0, 3, -8 };
            _playerList.Sort(arr);
            Assert.AreEqual(7, arr.Length);
            Assert.AreEqual(-8, arr[0]);
            Assert.AreEqual(-7, arr[1]);
            Assert.AreEqual(-3, arr[2]);
            Assert.AreEqual(0, arr[3]);
            Assert.AreEqual(2, arr[4]);
            Assert.AreEqual(3, arr[5]);
            Assert.AreEqual(5, arr[6]);
        }

        [Test]
        public void SortsAlreadySortedArray()
        {
            int[] arr = { 1, 2, 3, 4, 5 };
            _playerList.Sort(arr);
            Assert.AreEqual(5, arr.Length);
            Assert.AreEqual(1, arr[0]);
            Assert.AreEqual(2, arr[1]);
            Assert.AreEqual(3, arr[2]);
            Assert.AreEqual(4, arr[3]);
            Assert.AreEqual(5, arr[4]);
        }

        #endregion
    }
}