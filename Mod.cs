using System;
using System.Collections;
using UnityEngine;
using VRC.Core;
using VRC.UI;
using VRCSDK2;
using VRCModLoader;
using VRCMenuUtils;
using VRChat.UI;

namespace PortalToFriend
{
    [VRCModInfo("Portal to Friend", "2.0", ModData.Authors)]
    public class Mod : VRCMod
    {
        public void OnApplicationStart()
        {
            ModManager.StartCoroutine(Setup());
        }
        public IEnumerator Setup()
        {
            yield return VRCMenuUtilsAPI.WaitForInit();

            var DropPortalButton = new VRCEUiButton("Portal", Vector2.zero, "Drop Portal");
            DropPortalButton.OnClick += () =>
            {
                //VRCMenuUtilsAPI.GetPage(VRCEUi.UserInfoScreen.name).GetComponentInChildren<PageUserInfo>().user;
                APIUser usr = VRCEUi.UserInfoScreen.GetComponentInChildren<PageUserInfo>().user;
                if (usr.id == APIUser.CurrentUser.id)
                {
                    VRCMenuUtilsAPI.VRCUiPopupManager.ShowAlert("Error", "You cannot drop a portal to yourself!");
                    return;
                }
                else if (string.IsNullOrEmpty(usr.location))
                {
                    VRCMenuUtilsAPI.VRCUiPopupManager.ShowAlert("Error", usr.displayName + " has no valid location!");
                    return;
                }
                else if (usr.location.ToLower() == "private")
                {
                    VRCMenuUtilsAPI.VRCUiPopupManager.ShowAlert("Error", usr.displayName + " is in a private world!");
                    return;
                }
                else if (usr.location.ToLower() == "offline")
                {
                    VRCMenuUtilsAPI.VRCUiPopupManager.ShowAlert("Error", usr.displayName + " is offline!");
                    return;
                }
                string id = usr.location;
                string[] instance = id.Split(':');
                if (instance.Length == 2)
                { DropPortalToWorld(instance[0], instance[1]); }
                else
                { DropPortalToWorld(id); }
            };
            VRCMenuUtilsAPI.AddUserInfoButton(DropPortalButton);
        }

        public static void DropPortalToWorld(string worldid, string instanceid = "1337", int instancecount = 1)
        {
            GameObject portal = Networking.Instantiate(VRC_EventHandler.VrcBroadcastType.Always, "Portals/PortalInternalDynamic", VRCPlayer.Instance.transform.position + VRCPlayer.Instance.transform.forward * 2, VRCPlayer.Instance.transform.rotation);
            Networking.RPC(VRC_EventHandler.VrcTargetType.AllBufferOne, portal, "ConfigurePortal", new object[]
            {
                worldid,
                instanceid,
                instancecount
            });
        }
    }
}
