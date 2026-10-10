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

        /// <summary>
        /// Point-and-pull: walk near a thing, hold a hand out above it pointing down at it, close
        /// the hand, and wait for it to arrive. Fails if something else arrives instead.
        /// </summary>
        private IEnumerator PullToHand(bool left, GameObject target)
        {
            Bounds bounds = new Bounds(target.transform.position, Vector3.zero);
            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>())
                bounds.Encapsulate(renderer.bounds);
            yield return _robot.WalkTo(bounds.center, 0.75f);
            Vector3 back = Vector3.ProjectOnPlane(_robot.Head.position - bounds.center, Vector3.up).normalized;
            Vector3 hand = bounds.center + Vector3.up * 0.5f + back * 0.3f;
            yield return _robot.MoveHandWorld(left, hand, Quaternion.LookRotation(bounds.center - hand), 0.6f);
            yield return Wait(0.3f);
            _robot.SetGrip(left, true);
            Hand holding = left ? _robot.LeftHand : _robot.RightHand;
            for (float t = 0f; t < 2.5f && holding.CurrentGrab == null; t += Time.deltaTime)
                yield return null;
            Assert.IsNotNull(holding.CurrentGrab, $"pointing at {target.name} and closing the hand brought nothing");
            Rigidbody got = holding.CurrentGrab.GetComponentInParent<Rigidbody>();
            Assert.AreEqual(target.name, got != null ? got.name : "nothing", "point-and-pull brought the wrong thing");
            yield return Wait(0.3f);
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

        [UnityTest]
        public IEnumerator ShootsTheBowAtTheDummy()
        {
            const string dir = OutDir + "/bow";
            GameObject bow = GameObject.Find("wpn_bow_willow");
            Assert.IsNotNull(bow, "no bow in the scene");
            Rigidbody torso = GameObject.Find("Torso").GetComponent<Rigidbody>();
            Collider grip = bow.GetComponentInChildren<Grab>().GetComponent<Collider>();

            // Take the bow the way a player does from a crowded table: point at it and pull.
            yield return PullToHand(true, bow);

            // Stand a few metres from the dummy and shoot five arrows at its chest.
            Vector3 stand = torso.position + new Vector3(0.6f, 0f, -3.2f);
            yield return _robot.MoveHandBody(true, new Vector3(-0.25f, 1.1f, 0.35f), Quaternion.identity, 0.4f);
            yield return _robot.WalkTo(stand, 0.25f);
            int flew = 0, stuckInDummy = 0, stuckNearHand = 0;
            for (int shot = 0; shot < 5; shot++)
            {
                Vector3 chest = torso.position + Vector3.up * 0.15f;
// Draw the way an archer does: the string hand anchors beside the jaw, the bow hand
                // goes out along the line from that anchor to the target.
                Vector3 toTarget = Vector3.ProjectOnPlane(chest - _robot.Head.position, Vector3.up).normalized;
                Vector3 rightOfHead = Vector3.Cross(Vector3.up, toTarget);
                Vector3 anchor = _robot.Head.position + rightOfHead * 0.16f - Vector3.up * 0.10f + toTarget * 0.05f;
                Vector3 aim = (chest - anchor).normalized;
                Vector3 bowHand = anchor + aim * 0.60f;
                _robot.LookAt(chest);
                yield return _robot.MoveHandWorld(true, bowHand, Quaternion.LookRotation(aim), 0.4f);
                yield return _robot.MoveHandWorld(false, bowHand - aim * 0.12f, Quaternion.LookRotation(aim), 0.4f);
                yield return Wait(0.2f);
                _robot.SetTrigger(false, 1f);
                yield return Wait(0.15f);
                yield return _robot.MoveHandWorld(false, anchor, Quaternion.LookRotation(aim), 0.5f);
                yield return Wait(0.3f);
                // The arrow lies from palm to palm, not controller to controller. Correct the bow
                // hand until the nocked arrow points at the target, as a person does by eye.
                Vector3 bowTarget = bowHand;
                for (int correction = 0; correction < 4; correction++)
                {
                    Vector3 stringPalm = _robot.RightHand.PalmTransform.position;
                    Vector3 wanted = stringPalm + (chest - stringPalm).normalized * 0.58f;
                    bowTarget += wanted - _robot.LeftHand.PalmTransform.position;
                    yield return _robot.MoveHandWorld(true, bowTarget, Quaternion.LookRotation(aim), 0.2f);
                    yield return Wait(0.25f);
                }
                aim = (chest - _robot.RightHand.PalmTransform.position).normalized;
                Vector3 actual = (_robot.LeftHand.PalmTransform.position - _robot.RightHand.PalmTransform.position).normalized;                Debug.Log($"[Robot] shot {shot + 1}: the hands point {Vector3.Angle(actual, aim):F1} degrees off the intended line, draw {Vector3.Distance(_robot.LeftHand.PalmTransform.position, _robot.RightHand.PalmTransform.position):F2} m");                yield return Wait(0.2f);
                if (shot == 0)
                {
                    _robot.Snapshot(dir + "/01_drawn.jpg", new Vector3(1.6f, 1.5f, 0.4f));
                    _robot.SnapshotEyes(dir + "/02_eyes_drawn.jpg");
                }
                Vector3 loosedFrom = _robot.LeftHand.PalmTransform.position;
                _robot.SetTrigger(false, 0f);
                yield return Wait(1.0f);

                // Where did that arrow end up?
                GameObject newest = null;
                foreach (GameObject candidate in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                    if (candidate.name == "Arrow")
                        newest = candidate;
                Assert.IsNotNull(newest, "releasing the string made no arrow");
                float travelled = Vector3.Distance(newest.transform.position, loosedFrom);
                bool inDummy = newest.transform.parent != null && newest.transform.parent.GetComponentInParent<ImpactFlash>() != null;
                string where = newest.transform.parent != null ? newest.transform.parent.name : "flying or on the ground";
                Debug.Log($"[Robot] arrow {shot + 1}: {travelled:F2} m from the bow hand, in: {where}");
                if (inDummy) stuckInDummy++;
                else if (travelled < 1.0f) stuckNearHand++;
                else flew++;
                newest.name = "Arrow (counted)";
            }
            _robot.Snapshot(dir + "/03_after.jpg", new Vector3(1.6f, 1.5f, 0.4f), 1.0f, torso.position);
            _robot.SnapshotEyes(dir + "/04_eyes_after.jpg");
            Debug.Log($"[Robot] bow routine: {stuckInDummy} in the dummy, {flew} flew elsewhere, {stuckNearHand} stuck near the hand");
            Assert.AreEqual(0, stuckNearHand, "arrows stuck where they were loosed instead of flying");
            Assert.GreaterOrEqual(stuckInDummy, 3, "at least three of five aimed arrows should hit a dummy three metres away");
        }    }
}
