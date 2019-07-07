using System;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VRC.Core;
using VRC.UI;
using VRCSDK2;
using VRLoader.Attributes;
using VRLoader.Modules;

namespace PortalToFriend
{
    [ModuleInfo("Drop portal to instance", "1.1", "bay, yoshifan, plu")]
    public class Mod : VRModule
    {
        private static ModuleInfoAttribute ModInfo = Attribute.GetCustomAttribute(typeof(Mod), typeof(ModuleInfoAttribute)) as ModuleInfoAttribute;
        public const string userPanelString = "MenuContent/Screens/UserInfo/User Panel/";
        private bool initialized = false;
        
        void Awake() {
            if (initialized) return;
            var userPanel = GameObject.Find(userPanelString);
            // Utils.Log("Mod.Awake and not initialized");
            if (userPanel is null) return;
            initialized = true;
            Init();
        }

        private void Init() {
            Utils.Log($"Initializing {ModInfo.Name} v{ModInfo.Version} by {ModInfo.Author}");
            new GameObject("DropPortalToFriend").AddComponent<ModComponent>();
        }
    }

    class ModComponent : MonoBehaviour
    {
        public GameObject UserDropPortalButton { get; private set; }
        public static VRCUiManager VrcuimInstance { get; private set; }
        public static VRCUiPopupManager PopupManagerInstance;
        void Awake()
        {
            Utils.Log("ModComponent.Awake");
            DontDestroyOnLoad(this);
            var userPanel = GameObject.Find(Mod.userPanelString);
            // userPanel.transform.position += new Vector3(0, 0.065f, 0);
            var playlistButton = GameObject.Find("MenuContent/Screens/UserInfo/User Panel/Playlists/PlaylistsButton");
            var playlists = GameObject.Find("MenuContent/Screens/UserInfo/User Panel/Playlists");
            UserDropPortalButton = Instantiate(playlistButton, playlists.transform);
            UserDropPortalButton.transform.SetParent(userPanel.transform);
            UserDropPortalButton.GetComponent<RectTransform>().anchoredPosition += new Vector2(0, 75);
            UserDropPortalButton.GetComponentInChildren<Text>().text = "Drop Portal";
            UserDropPortalButton.GetComponentInChildren<Button>().onClick = new Button.ButtonClickedEvent();
            UserDropPortalButton.GetComponentInChildren<Button>().onClick.AddListener(DropPortalToUserClicked);
        }

        private void DropPortalToUserClicked()
        {
            var userInfoPage = GetVRCUiMInstance().menuContent.GetComponentInChildren<PageUserInfo>();
            var player = userInfoPage.user;
            Utils.Log("Selected User:", player.displayName, player.id.Enclose());
            Utils.Log("Location:", player.location);
            if (player.id == APIUser.CurrentUser.id)
            {
                GetVRCUiPopupManager().ShowAlert("Error", "You cannot drop a portal to yourself!");
                return;
            }
            else if (player.location.IsNullOrEmpty())
            {
                GetVRCUiPopupManager().ShowAlert("Error", $"Player {player.displayName.Quote()} has no valid location!");
                return;
            }
            else if (player.location == "private")
            {
                GetVRCUiPopupManager().ShowAlert("Error", $"Player {player.displayName.Quote()} is in a private instance!");
                return;
            }
            else if (player.location == "local")
            {
                GetVRCUiPopupManager().ShowAlert("Error", $"Player {player.displayName.Quote()} is in a local test world!");
                return;
            }
            var location = player.location;
            string[] locationArray = location.Split(':');
            // DropPortalToLocation(locationArray);
            Action<ApiContainer> onSuccess = new Action<ApiContainer>(ApiWorldRecieved);
            Action<ApiContainer> onFailure = new Action<ApiContainer>(ApiWorldFailed);
            var world = API.Fetch<ApiWorld>(locationArray[0], onSuccess, onFailure, false);
            var instance = new ApiWorldInstance(world, locationArray[1], 0);
            Utils.Log("World: ", world.name, world.id.Enclose());
            var instancesstr = string.Join(";", world.instances.Select(x => $"{x.Key}={x.Value}").ToArray());
            Utils.Log("Instances:", instancesstr);
            if (world.instances.ContainsKey(instance.idOnly))
                instance.count = world.instances[instance.idOnly];
            else instance.count = -1;
            Utils.Log("Instance:", instance.idWithTags, $"({instance.count}/{world.capacity})");
            PortalInternal.CreatePortal(world, instance, VRCPlayer.Instance.transform.position, VRCPlayer.Instance.transform.forward, true);
        }

        internal static void ApiWorldRecieved(ApiContainer apiContainer) { }

        internal static void ApiWorldFailed(ApiContainer apiContainer) { }

        public static void DropPortalToLocation(string[] location, int count=0)
        {
            Utils.Log("Dropping Portal to :", location);
            var gameObject = Networking.Instantiate(VRC_EventHandler.VrcBroadcastType.Always, "Portals/PortalInternalDynamic", VRCPlayer.Instance.transform.position + VRCPlayer.Instance.transform.forward, VRCPlayer.Instance.transform.rotation);
            Networking.RPC(VRC_EventHandler.VrcTargetType.AllBufferOne, gameObject, "ConfigurePortal", new object[]
            {
                    location[0],
                    location[1],
                    count
            });
        }

        public static VRCUiManager GetVRCUiMInstance()
        {
            if (ModComponent.VrcuimInstance == null)
            {
                var method = typeof(VRCUiManager).GetMethod("get_Instance", BindingFlags.Static | BindingFlags.Public);
                if (method == null)
                {
                    return null;
                }
                ModComponent.VrcuimInstance = (VRCUiManager)method.Invoke(null, new object[0]);
            }
            return VrcuimInstance;
        }

        public static VRCUiPopupManager GetVRCUiPopupManager()
        {
            if (PopupManagerInstance == null)
            {
                FieldInfo[] nonpublicStaticPopupFields = typeof(VRCUiPopupManager).GetFields(BindingFlags.NonPublic | BindingFlags.Static);
                if (nonpublicStaticPopupFields.Length == 0)
                {
                    return null;
                }
                FieldInfo uiPopupManagerInstanceField = nonpublicStaticPopupFields.First(field => field.FieldType == typeof(VRCUiPopupManager));
                if (uiPopupManagerInstanceField == null)
                {
                    return null;
                }
                PopupManagerInstance = uiPopupManagerInstanceField.GetValue(null) as VRCUiPopupManager;
            }

            return PopupManagerInstance;
        }
    }
}
