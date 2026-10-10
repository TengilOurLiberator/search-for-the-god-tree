using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Godtree.Tests
{
    /// <summary>
    /// Headless checks on the VR sandbox scene. They prove the scene loads and its
    /// physics behaves; they cannot prove anything about how it feels in a headset.
    /// </summary>
    public sealed class SandboxSceneTests
    {
        private const string SceneName = "Sandbox";

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            // Without a headset the XR loader logs errors; those are not what is under test.
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return new WaitForSeconds(1.5f);
        }

        [UnityTest]
        public IEnumerator PhysicsRunsAt144Hz()
        {
            Assert.AreEqual(1f / 144f, Time.fixedDeltaTime, 1e-5f);
            yield break;
        }

        [UnityTest]
        public IEnumerator SwordRestsOnThePlinth()
        {
            GameObject sword = GameObject.Find("wpn_sword_training");
            Assert.IsNotNull(sword, "sword not found in scene");
            float height = sword.transform.position.y;
            Assert.That(height, Is.InRange(0.85f, 1.05f), "sword should be lying on the plinth at about hand height");
            Assert.Less(sword.GetComponent<Rigidbody>().linearVelocity.magnitude, 0.05f, "sword should have come to rest");
            yield break;
        }

        [UnityTest]
        public IEnumerator DummyStandsThenReactsToAHitAndRecovers()
        {
            Rigidbody torso = GameObject.Find("Torso").GetComponent<Rigidbody>();
            Vector3 restPosition = torso.position;
            Quaternion restRotation = torso.rotation;
            Assert.That(restPosition.y, Is.InRange(1.0f, 1.3f), "torso should be held up by its joint");

            // A solid sideways blow at chest height, about what a hard sword swing delivers.
            torso.AddForceAtPosition(Vector3.right * 60f, torso.worldCenterOfMass + Vector3.up * 0.2f, ForceMode.Impulse);
            float peakAngle = 0f;
            for (float t = 0f; t < 0.5f; t += Time.fixedDeltaTime)
            {
                yield return new WaitForFixedUpdate();
                peakAngle = Mathf.Max(peakAngle, Quaternion.Angle(restRotation, torso.rotation));
            }
            Assert.Greater(peakAngle, 3f, "the torso should visibly rock when struck");
            Assert.Less(peakAngle, 80f, "the torso should not fold flat from one blow");

            yield return new WaitForSeconds(3f);
            Assert.Less(Quaternion.Angle(restRotation, torso.rotation), 3f, "the torso should spring back upright");
            Assert.Less(Vector3.Distance(restPosition, torso.position), 0.05f, "the torso should return to its place");
        }

        [UnityTest]
        public IEnumerator SwordCanBeChosenByAHand()
        {
            // Regression: the first sword had a grab point inside its own collider, which the
            // rig ranks as ungrabbable, so it could never be picked up.
            GameObject sword = GameObject.Find("wpn_sword_training");
            BIMOS.Grab grab = sword.GetComponentInChildren<BIMOS.Grab>();
            Assert.IsNotNull(grab, "sword has no grab");
            Assert.IsNotNull(grab.GetComponent<Collider>(), "sword grab has no collider for a hand to find");
            var palm = new GameObject("TestPalm").transform;
            palm.position = grab.GetComponent<Collider>().bounds.center + Vector3.up * 0.1f;
            Assert.Greater(grab.CalculateRank(palm), 0f, "a hand beside the sword must be able to choose it");
            Object.Destroy(palm.gameObject);
            yield break;
        }
        [UnityTest]
        public IEnumerator TableWeaponsAndLoosePropsAreGrabbableAndStayPut()
        {
            BIMOS.Grab[] grabs = Object.FindObjectsByType<BIMOS.Grab>(FindObjectsSortMode.None);
            Assert.GreaterOrEqual(grabs.Length, 8, "weapons on the table and loose props should all be grabbable");
            foreach (BIMOS.Grab grab in grabs)
            {
                Rigidbody body = grab.GetComponentInParent<Rigidbody>();
                Assert.IsNotNull(body, grab.name + " has a grab but no rigidbody");
                Assert.IsNotNull(body.GetComponentInChildren<Collider>(), grab.name + " has a grab but no collider");
                Assert.Greater(body.position.y, -0.5f, body.name + " fell through the floor");
            }
            yield break;
        }

        [UnityTest]
        public IEnumerator InstructionBoardExists()
        {
            Assert.IsNotNull(GameObject.Find("How to play (board)"), "the instruction board is missing");
            yield break;
        }

        [UnityTest]
        public IEnumerator PlayerRigSpawns()
        {
            Assert.Greater(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length, 0, "the BIMOS rig should have spawned a camera");
            Assert.GreaterOrEqual(LayerMask.NameToLayer("BIMOSRig"), 0, "the BIMOSRig layer must exist");
            yield break;
        }
    }
}
