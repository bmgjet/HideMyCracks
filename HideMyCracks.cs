using UnityEngine;
namespace Oxide.Plugins
{
    [Info("HideMyCracks", "bmgjet", "1.0.1")]
    [Description("Hides the light shining though cracks")]
    public class HideMyCracks : RustPlugin
    {
        private const string UsePermission = "HideMyCracks.use";
        private void OnPlayerRespawn(BasePlayer current)
        {
            //Attaches mod to player created
            AddHideCracksMod(current);
        }

        private void Init()
        {
            permission.RegisterPermission(UsePermission, this);
        }

        void OnServerInitialized()
        {
            foreach (BasePlayer current in BasePlayer.activePlayerList)
            {
                //Cycles though list of active players.
                AddHideCracksMod(current);
            }
        }

        void OnPlayerSleepEnded(BasePlayer current)
        {
            //Attaches mod to player waking up
            AddHideCracksMod(current);
        }

        void AddHideCracksMod(BasePlayer player)
        {
            if (player.GetComponent<HideCracksMod>() == null && !player.IsNpc)
            {
                //Checks they dont already have mod and arnt a NPC and have permission to use it.
                player.gameObject.AddComponent<HideCracksMod>();
            }
        }

        void Unload()
        {
            //Removees all mods.
            var objects = GameObject.FindObjectsOfType(typeof(HideCracksMod));
            if (objects != null)
            {
                foreach (var gameObj in objects)
                {
                    GameObject.Destroy(gameObj);
                }
            }
        }

        public static bool HasPerm(BasePlayer player)
        {
            HideMyCracks permcheck = new HideMyCracks();
            return permcheck.permission.UserHasPermission(player.UserIDString, UsePermission);
        }

        private class HideCracksMod : FacepunchBehaviour
        {
            private BasePlayer _player;
            private bool _darkened = false;
            private bool _isadmin;
            public bool debug = false;
            private Vector3 oldpos;
            public string[] Triggers = new string[]
            {
            "road_tunnel_double_str_a_36m",
            "road_tunnel_double_slope_b_72m",
            //"terrain_trigger"
            };
            private void Awake()
            {
                _player = GetComponent<BasePlayer>();
                oldpos = _player.transform.localPosition;
                _isadmin = _player.IsAdmin;
                InvokeRepeating(Check, 0.5f, 0.5f);
            }

            private void ChangeTime(int time)
            {
                if (!_isadmin)
                {
                    //toggle admin so admintime command works on them.
                    _player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
                    _player.SendNetworkUpdateImmediate();
                }
                _player.SendConsoleCommand("admintime", time); //Sets there time 
                if (!_isadmin && _player.HasPlayerFlag(BasePlayer.PlayerFlags.IsAdmin))
                {
                    _player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                    _player.SendNetworkUpdateImmediate();
                }
            }

            private void Check()
            {
                if (_player == null || !_player.IsConnected || _player.IsSleeping() || !HideMyCracks.HasPerm(_player))
                {
                    return; //Return as soon as posiable if not useful.
                }
                //Dont keep checking if player not moving
                if (Vector3.Distance(_player.transform.position, oldpos) < 8f)
                {
                    return;
                }
                oldpos = _player.transform.localPosition;

                Vector3 forward = oldpos;
                //Gets a location infront/behind the player to ehance transition quality
                if (_darkened)
                {
                    //trigger behind
                    forward += _player.eyes.BodyForward() * 2;
                }
                else
                {
                    //trigger infront
                    forward -= _player.eyes.BodyForward() * 2;
                }
                forward.y += 0.5f;
                if (debug && _player.IsAdmin)
                {
                    //Draws where the players trigger point is.
                    _player.SendConsoleCommand("ddraw.sphere", 8f, Color.red, forward, 0.5f);
                }
                var hits = Physics.SphereCastAll(forward, 0.5f, Vector3.one);
                foreach (var hit in hits)
                {
                    Collider bc = hit.GetCollider();
                    if (bc == null)
                    {
                        continue;
                    }
                    //if (debug) //Dump names of triggers into player chat
                    //{
                    //    _player.ChatMessage(bc.name);
                    //}
                    foreach (string t in Triggers) //Go though array of triggers.
                    {
                        if (bc.name.Contains(t) && !_darkened) //Checks player is in trigger and not already darkened
                        {
                            ChangeTime(24);
                            _darkened = true;
                            return; //return from thread as soon as can.
                        }
                        else if (bc.name.Contains(t) && _darkened) //Checks player is in trigger and not already darkened
                        {
                            return; //stop searching and return
                        }
                    }
                }
                //Gets to this point if no trigger was found
                if (_darkened)
                {
                    ChangeTime(-1);
                    _darkened = false;
                }
            }
        }
    }
}