using System.Collections;
using BIMOS;
using Godtree.Combat;
using Godtree.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Godtree.Tests
{
    /// <summary>
    /// Play sessions run by the robot player (Scripts/Diagnostics/RobotPlayer.cs): nobody in a
    /// headset, the rig driven by script through the real input actions. Each routine does what a
    /// player would do, checks that it worked, and saves pictures to D:/godtree-work/robot/ for
    /// the lead to look at. They show whether a thing works; they say nothing about how it feels.
    /// </summary>
    [Category("Robot")]
    public sealed class RobotPlaytests
    {
        private const string OutDir = "D:/godtree-work/robot";
        private RobotPlayer _robot;
        private int _hits;
        private float _hardestHit;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync("Sandbox", LoadSceneMode.Single);
            yield return new WaitForSeconds(1.0f);
            _robot = RobotPlayer.Create();
            float waited = 0f;
            while (!_robot.Ready && waited < 10f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(_robot.Ready, "the robot player could not take over the rig");
            _hits = 0;
            _hardestHit = 0f;
            ImpactFlash.Hit += CountHit;
            yield return new WaitForSeconds(0.5f);
        }

        [TearDown]
        public void Stop()
        {
            ImpactFlash.Hit -= CountHit;
        }

        private void CountHit(string struck, string striker, float impulse)
        {
            _hits++;
            _hardestHit = Mathf.Max(_hardestHit, impulse);
            Debug.Log($"[Robot] hit: {struck} by {striker}, {impulse:F1} N s");
        }

        private static IEnumerator Wait(float seconds)
        {
            yield return new WaitForSeconds(seconds);
        }

        [UnityTest]
        public IEnumerator ButtonsReachTheRig()
        {
            _robot.SetGrip(false, true);
            _robot.SetTrigger(true, 0.8f);
            yield return Wait(0.3f);
            Assert.Greater(_robot.RightHand.HandInputReader.Grip, 0.9f, "right grip did not arrive");
            Assert.Greater(_robot.LeftHand.HandInputReader.Trigger, 0.7f, "left trigger did not arrive");
            Assert.Less(_robot.LeftHand.HandInputReader.Grip, 0.1f, "left grip should be untouched");
            _robot.SetGrip(false, false);
            _robot.SetTrigger(true, 0f);
            yield return Wait(0.2f);
            Assert.Less(_robot.RightHand.HandInputReader.Grip, 0.1f, "right grip did not release");

            // Hands follow their targets.
            yield return _robot.MoveHandBody(false, new Vector3(0.25f, 1.3f, 0.45f), Quaternion.identity, 0.6f);
            yield return Wait(0.6f);
            float gap = Vector3.Distance(_robot.RightHand.PhysicsHandTransform.position, _robot.HandTargetWorld(false));
            Debug.Log($"[Robot] head {_robot.Head.position}, right hand gap {gap:F3} m");
            Assert.Less(gap, 0.25f, "the physics hand did not follow the controller");
            _robot.Snapshot(OutDir + "/buttons/01_stand.jpg", new Vector3(1.6f, 1.5f, 2.2f));
            _robot.SnapshotEyes(OutDir + "/buttons/02_eyes.jpg");
        }

        [UnityTest]
        public IEnumerator TakesTheSwordAndHitsTheDummy()
        {
            const string dir = OutDir + "/sword";
            GameObject sword = GameObject.Find("wpn_sword_training");
            Rigidbody torso = GameObject.Find("Torso").GetComponent<Rigidbody>();
            Collider grip = sword.GetComponentInChildren<Grab>().GetComponent<Collider>();
            Debug.Log($"[Robot] feet {_robot.Feet}, sword grip {grip.bounds.center}, dummy {torso.position}");

            // Reach down to the sword from above, palm down, and close the hand.
            Vector3 above = grip.bounds.center + Vector3.up * 0.12f;
            Quaternion palmDown = Quaternion.Euler(60f, 0f, 0f);
            yield return _robot.MoveHandWorld(false, above, palmDown, 0.8f);
            yield return _robot.MoveHandWorld(false, grip.bounds.center + Vector3.up * 0.03f, palmDown, 0.4f);
            yield return Wait(0.3f);
            _robot.Snapshot(dir + "/01_reach.jpg", new Vector3(-1.8f, 1.5f, 0.6f));
            _robot.SetGrip(false, true);
            yield return Wait(0.5f);
            Assert.IsNotNull(_robot.RightHand.CurrentGrab, "closing the hand on the sword did not pick it up");

            // Carry it at the side, walk up to the dummy, then strike across its chest three times.
            yield return _robot.MoveHandBody(false, new Vector3(0.28f, 1.0f, 0.3f), Quaternion.Euler(-40f, 0f, 0f), 0.5f);
            yield return _robot.WalkTo(torso.position, 1.05f);
            Debug.Log($"[Robot] walked to {_robot.Feet}, {_robot.GroundDistanceTo(torso.position):F2} m from the dummy");
            Assert.Less(_robot.GroundDistanceTo(torso.position), 1.3f, "the robot could not walk up to the dummy");
            Assert.IsNotNull(_robot.RightHand.CurrentGrab, "the sword was dropped while walking");
            Vector3 chest = torso.position + Vector3.up * 0.1f;
            Vector3 toDummy = Vector3.ProjectOnPlane(chest - _robot.Head.position, Vector3.up).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, toDummy);
            Vector3 handHome = _robot.Feet + toDummy * 0.35f + Vector3.up * 0.95f; // sack height, not head height
            Quaternion aim = Quaternion.LookRotation(toDummy);
            for (int swing = 0; swing < 3; swing++)
            {
                yield return _robot.MoveHandWorld(false, handHome + side * 0.45f + Vector3.up * 0.25f, aim * Quaternion.Euler(-50f, 60f, 0f), 0.5f);
                yield return Wait(0.2f);
                if (swing == 0)
                    _robot.Snapshot(dir + "/02_raised.jpg", new Vector3(-1.8f, 1.5f, 0.6f));
                yield return _robot.MoveHandWorld(false, handHome - side * 0.35f + toDummy * 0.25f, aim * Quaternion.Euler(20f, -60f, 0f), 0.16f);
                if (swing == 0)
                    _robot.Snapshot(dir + "/03_struck.jpg", new Vector3(-1.8f, 1.5f, 0.6f));
                yield return Wait(0.5f);
            }
            _robot.SnapshotEyes(dir + "/04_eyes.jpg");
            Debug.Log($"[Robot] sword routine: {_hits} hits, hardest {_hardestHit:F1} N s");
            Assert.Greater(_hits, 0, "three swings at the dummy landed nothing");

            // Let go: the sword must leave the hand.
            _robot.SetGrip(false, false);
            yield return Wait(0.5f);
            Assert.IsNull(_robot.RightHand.CurrentGrab, "opening the hand did not drop the sword");
        }
    }
}
