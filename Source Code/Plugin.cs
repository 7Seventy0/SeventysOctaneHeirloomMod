using BepInEx;
using System;
using UnityEngine;
using Utilla;
using System.Collections;
using System.Reflection;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using BepInEx.Configuration;
using System.IO;

namespace SeventysOctaneHeirloomMod
{
    /// <summary>
    /// This is your mod's main class.
    /// </summary>

    /* This attribute tells Utilla to look for [ModdedGameJoin] and [ModdedGameLeave] */
    [ModdedGamemode]
    [BepInDependency("org.legoandmars.gorillatag.utilla", "1.5.0")]
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        bool inRoom;
        GameObject knifeInstance;
        bool modActive;
        bool isStimmed;
        GameObject stimSound;
        GameObject stimVisuals;
        GameObject trailEffects;
        GameObject trailEffectsInstance;
        Image visual;

        public static ConfigEntry<bool> greenVisual;
        public static ConfigEntry<bool> particle;
        public static ConfigEntry<float> stimVolume;
        void Start()
        {
            ConfigFile config = new ConfigFile(Path.Combine(Paths.ConfigPath, "SeventysOctaneHeirloom.cfg"), true);
            greenVisual = config.Bind<bool>("Config", "Green Visual?", true, "Should your screen Flash Green when stimmed up?");
            particle = config.Bind<bool>("Config", "Particles?", true, "Should you cast particles when stimmed up??");
            stimVolume = config.Bind<float>("Config", "Volume of Stim Sound", 0.06f, "Changes only the Volume of the Stim effect");
        }
        void OnEnable()
        {
            /* Set up your mod here */
            /* Code here runs at the start and whenever your mod is enabled*/
            modActive = true;
            if(knifeInstance == null)
            {
                StartCoroutine(SeventysStart());
            }
            else
            {
                ApplyCosmetic();
            }
            HarmonyPatches.ApplyHarmonyPatches();
            Utilla.Events.GameInitialized += OnGameInitialized;
        }

        void OnDisable()
        {
            /* Undo mod setup here */
            /* This provides support for toggling mods with ComputerInterface, please implement it :) */
            /* Code here runs whenever your mod is disabled (including if it disabled on startup)*/
            if(knifeInstance != null)
            {
                UnApply();
            }
            modActive = false;
            HarmonyPatches.RemoveHarmonyPatches();
            Utilla.Events.GameInitialized -= OnGameInitialized;
            GorillaLocomotion.Player.Instance.velocityLimit = 0.3f;
        }

        void OnGameInitialized(object sender, EventArgs e)
        {
            /* Code here runs after the game initializes (i.e. GorillaLocomotion.Player.Instance != null) */
        }
        IEnumerator SeventysStart()
        {
            var fileStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SeventysOctaneHeirloomMod.Assets.octanebundle");
            var bundleLoadRequest = AssetBundle.LoadFromStreamAsync(fileStream);
            yield return bundleLoadRequest;

            var myLoadedAssetBundle = bundleLoadRequest.assetBundle;
            if (myLoadedAssetBundle == null)
            {
                Debug.Log("Failed to load AssetBundle!");
                yield break;
            }
            
            var assetLoadRequest = myLoadedAssetBundle.LoadAssetAsync<GameObject>("Octane");
            yield return assetLoadRequest;

            GameObject octane = assetLoadRequest.asset as GameObject;
            knifeInstance = Instantiate(octane);

            assetLoadRequest = myLoadedAssetBundle.LoadAssetAsync<GameObject>("StimSound");
            yield return assetLoadRequest;

           stimSound = assetLoadRequest.asset as GameObject;

            assetLoadRequest = myLoadedAssetBundle.LoadAssetAsync<GameObject>("StrimVisualCanvas");
            yield return assetLoadRequest;

            stimVisuals = assetLoadRequest.asset as GameObject;

            assetLoadRequest = myLoadedAssetBundle.LoadAssetAsync<GameObject>("StimTrails");
            yield return assetLoadRequest;

            trailEffects = assetLoadRequest.asset as GameObject;

            ApplyCosmetic();
        }

        void ApplyCosmetic()
        {
            GameObject hand = GameObject.Find("palm.01.R");
            knifeInstance.transform.SetParent(hand.transform, false);
            knifeInstance.transform.localPosition = new Vector3(-0.026f, 0.06f, - 0.05f);
            knifeInstance.transform.localEulerAngles = new Vector3(317.2502f, 167.1135f, 275.1937f);
            knifeInstance.transform.localScale = new Vector3(0.04f, 0.04f, 0.04f);
        }
        void UnApply()
        {
            knifeInstance.transform.parent = null;
            knifeInstance.transform.position = Vector3.zero;
        }
        float nextUseTime;

        float coolDown = 12;


        private readonly XRNode rNode = XRNode.RightHand;
        float stimTime;

        float gamerFloat;
        float minValue = 0.01f;
        float maxValue = 0.05f;
        float animTime = 1;
        void Update()
        {

            bool trigger;

            InputDevices.GetDeviceAtXRNode(rNode).TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out trigger);
            if (modActive && inRoom)
            {
                if(Time.time > nextUseTime)
                {
                    if (trigger)
                    {
                        Stim();
                        Debug.Log("Im going in, UAHHRG!!!");
                        nextUseTime = Time.time + coolDown;
                    }
                }
            }

            if (isStimmed && inRoom  && modActive)
            {
                GorillaLocomotion.Player.Instance.maxJumpSpeed = 12;
                GorillaLocomotion.Player.Instance.jumpMultiplier = 1.5f;
                GorillaLocomotion.Player.Instance.velocityLimit = 0;
            }
            
            if (isStimmed)
            {
              
                stimTime -= Time.deltaTime;
                if(stimTime <= 0)
                {
                    isStimmed = false;
                }
            }
            if (!isStimmed)
            {
                if(visualsInstance != null)
                {
                Destroy(visualsInstance);
                   

                }
                if(trailEffectsInstance != null)
                {
                    Destroy(trailEffectsInstance);
                }
            }

            if(visual != null)
            {
                visual.color = new Color(visual.color.r, visual.color.g, visual.color.b, gamerFloat);
                LeanTween.value(gamerFloat, maxValue, animTime)
                    .setEaseInOutCubic()
                    .setLoopPingPong()
                    .setOnUpdate(UpdateValue);
            }
        }
        GameObject visualsInstance;
        void UpdateValue(float from)
        {
            gamerFloat = from;
        }

        void Stim()
        {
            stimTime = 10;
            isStimmed = true;
            stimSound.GetComponent<AudioSource>().volume = stimVolume.Value;
            Instantiate(stimSound);
            if (greenVisual.Value)
            {
                visualsInstance = Instantiate(stimVisuals);
            visualsInstance.transform.SetParent(Camera.main.transform, false);
            visualsInstance.transform.localPosition = new Vector3(0,0,0.1f);
            }
            if (particle.Value)
            {
            trailEffectsInstance = Instantiate(trailEffects);
            trailEffectsInstance.transform.SetParent(Camera.main.transform, false);
            trailEffectsInstance.transform.localPosition = new Vector3(0,0,-0.25f);

            }
            gamerFloat = minValue;

            visual = GameObject.Find("StrimGreenThingy").GetComponent<Image>();
        }
        /* This attribute tells Utilla to call this method when a modded room is joined */
        [ModdedGamemodeJoin]
        public void OnJoin(string gamemode)
        {
            /* Activate your mod here */
            /* This code will run regardless of if the mod is enabled*/

            inRoom = true;
        }

        /* This attribute tells Utilla to call this method when a modded room is left */
        [ModdedGamemodeLeave]
        public void OnLeave(string gamemode)
        {
            /* Deactivate your mod here */
            /* This code will run regardless of if the mod is enabled*/

            inRoom = false;
            GorillaLocomotion.Player.Instance.velocityLimit = 0.3f;

        }
    }
}
