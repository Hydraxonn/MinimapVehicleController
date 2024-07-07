using GTA;
using GTA.Native;
using System;
using Ini;
using System.Windows.Forms;
using GTA.UI;
namespace MinimapVehicleController
{
    public class MinimapVehicleControllerMod : Script
    {
        public enum mapStates 
        {
            original,
            big,
            zoomout,
            full,
            hidden
        };
        public enum signalTypes
        {
            left,
            right,
            hazard
        };
        public static Keys CurrentWindowKey,PassengerWindowKey,DriverRearWindowKey,PassengerRearWindowKey,AllWindowsKey,HoodKey,TrunkKey,InteriorLightKey,ToggleRadioWheelKey,ToggleMobileRadioKey,ToggleMinimapKey,OpenDoorKey,SeatbeltKey,ShuffleSeatKey,LeftSignalKey, RightSignalKey, HazardsKey, RedLaserKey, GreenLaserKey;
        private readonly IniFile settingsINI;
        public static bool LightOff = true;
        public static bool MMoriginalEnabled, MMbigMapEnabled, MMzoomoutEnabled, MMfullEnabled, MMhiddenEnabled, enableVehicleControls, enableMinimapControls, enableMobileRadio, enablePhoneColor = true;
        public static bool leftSignalActive, rightSignalActive, hazardsActive, radioWheelDisabled, mobileRadio, beltedUp, isCurrentlyShuffling, window0down, window1down, window2down, window3down, AWindowDown, hoodOpen, trunkOpen, door0Open, door1Open, door2Open, door3Open, initialized = false;
        public static mapStates mapState = mapStates.original;
        public static int phoneColorIndex, MMsafetyVal = 0;
        //LASER COMPONENT HASHES
        public static WeaponComponentHash laserhash = (WeaponComponentHash)2455710022; //COMPONENT_AT_AR_LASER
        public static WeaponComponentHash redlaserhash = (WeaponComponentHash)1073457922; //COMPONENT_AT_AR_LASER_RED
        public static WeaponComponentHash laserhashREH = (WeaponComponentHash)2248128277; //COMPONENT_AT_AR_LASER_REH
        public static WeaponComponentHash redlaserhashREH = (WeaponComponentHash)3620657966; //COMPONENT_AT_AR_LASER_RED_REH
        public static WeaponComponentHash PIlaserhash = (WeaponComponentHash)1230280991; //COMPONENT_AT_PI_LASER
        public static WeaponComponentHash PIlaserhashIR = (WeaponComponentHash)743205558; //COMPONENT_AT_PI_LASER_IR
        public MinimapVehicleControllerMod(){
            if (!initialized){
                this.settingsINI = new IniFile("scripts/MVC.ini");
                LoadExternalSettings(settingsINI);
                MinimapSafetyCheck();
                initialized = true;
            }
            Tick += OnTick;
            KeyDown += OnKeyDown;
        }
        private void OnTick(object sender, EventArgs e){
            //PHONE COLOR
            if (enablePhoneColor){
                switch (Game.Player.Character.Model.Hash){
                    case 225514697: //Player zero
                        break;
                    case -1692214353: //player_one
                        break;
                    case -1686040670: //player_two
                        break;
                    default:
                        Function.Call(Hash.SET_PLAYER_PHONE_PALETTE_IDX, Game.Player, phoneColorIndex);
                        break;
                }
            }
            if (mapState == mapStates.hidden){
                Function.Call(Hash.HIDE_HUD_AND_RADAR_THIS_FRAME);
                Function.Call(Hash.HIDE_HELP_TEXT_THIS_FRAME);
                Function.Call(Hash.THEFEED_HIDE_THIS_FRAME);
            }
            if (beltedUp){Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, 75, true);}
            if (radioWheelDisabled){Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, 85, true);}
        }
        void OnKeyDown(object sender, KeyEventArgs e){
            switch (e.KeyCode){
                case var value when value == ToggleMobileRadioKey:
                    if (enableMobileRadio) { TogglePlayerMobileRadio(); }
                    break;
                case var value when value == ToggleRadioWheelKey:
                    if (enableMobileRadio) { ToggleRadioWheel(); }
                    break;
                case var value when value == ToggleMinimapKey:
                    if (enableMinimapControls && MMsafetyVal > 1) { CycleMinimapState(); }
                    break;
                case var value when value == OpenDoorKey:
                    if (enableVehicleControls) { OpenLocalDoor(Game.Player.Character.CurrentVehicle); }
                    break;
                case var value when value == ShuffleSeatKey:
                    if (enableVehicleControls) { ShuffleToNextSeat(Game.Player.Character.CurrentVehicle); }
                    break;
                case var value when value == RedLaserKey:
                    ToggleLaser(true);
                    break;
                case var value when value == GreenLaserKey:
                    ToggleLaser(false);
                    break;
            }
            if (Game.Player.Character.CurrentVehicle != null && enableVehicleControls) {
                switch (e.KeyCode){
                    case var value when value == HazardsKey:
                        ToggleTurnSignals(signalTypes.hazard, Game.Player.Character.CurrentVehicle);
                        break;
                    case var value when value == LeftSignalKey:
                        ToggleTurnSignals(signalTypes.left, Game.Player.Character.CurrentVehicle);
                        break;
                    case var value when value == RightSignalKey:
                        ToggleTurnSignals(signalTypes.right, Game.Player.Character.CurrentVehicle);
                        break;
                    case var value when value == SeatbeltKey:
                        TogglePlayerSeatbelt(Game.Player.Character.CurrentVehicle);
                        break;
                    case var value when value == InteriorLightKey:
                        ToggleInteriorLight(Game.Player.Character.CurrentVehicle, LightOff);
                        break;
                    case var value when value == CurrentWindowKey:
                        ToggleCurrentWindow(Game.Player.Character.CurrentVehicle);
                        break;
                    case var value when value == PassengerWindowKey:
                        DriverWindowAccess(Game.Player.Character.CurrentVehicle, 1);
                        break;
                    case var value when value == DriverRearWindowKey:
                        DriverWindowAccess(Game.Player.Character.CurrentVehicle, 2);
                        break;
                    case var value when value == PassengerRearWindowKey:
                        DriverWindowAccess(Game.Player.Character.CurrentVehicle, 3);
                        break;
                    case var value when value == AllWindowsKey:
                        ToggleAllWindows(Game.Player.Character.CurrentVehicle);
                        break;
                    case var value when value == HoodKey:
                        ToggleHood(Game.Player.Character.CurrentVehicle);
                        break;
                    case var value when value == TrunkKey:
                        ToggleTrunk(Game.Player.Character.CurrentVehicle);
                        break;
                }
            }
        }
        public static void LoadExternalSettings(IniFile settingsINI){
            phoneColorIndex = settingsINI.Read("CellPhoneColor", "MISC", 7);
            enableMinimapControls = settingsINI.Read("enableMinimapControls", "FEATURES", true);
            enableVehicleControls = settingsINI.Read("enableVehicleControls", "FEATURES", true);
            enableMobileRadio = settingsINI.Read("enableMobileRadio", "FEATURES", true);
            enablePhoneColor = settingsINI.Read("enablePhoneColor", "FEATURES", true);
            MMoriginalEnabled = settingsINI.Read("enableOriginalMapMode", "MAP MODES", true);
            MMbigMapEnabled = settingsINI.Read("enableBigMapMode", "MAP MODES", true);
            MMzoomoutEnabled = settingsINI.Read("enableZoomedOutMode", "MAP MODES", true);
            MMfullEnabled = settingsINI.Read("enableFullMode", "MAP MODES", true);
            MMhiddenEnabled = settingsINI.Read("enableHiddenMode", "MAP MODES", true);
            CurrentWindowKey = settingsINI.Read<Keys>("CurrentWindowKey", "KEYBINDS", Keys.D4);
            PassengerWindowKey = settingsINI.Read<Keys>("PassengerWindowKey", "KEYBINDS", Keys.D5);
            DriverRearWindowKey = settingsINI.Read<Keys>("DriverRearWindowKey", "KEYBINDS", Keys.D6);
            PassengerRearWindowKey = settingsINI.Read<Keys>("PassengerRearWindowKey", "KEYBINDS", Keys.D7);
            AllWindowsKey = settingsINI.Read<Keys>("AllWindowsKey", "KEYBINDS", Keys.D8);
            HoodKey = settingsINI.Read<Keys>("HoodKey", "KEYBINDS", Keys.D9);
            TrunkKey = settingsINI.Read<Keys>("TrunkKey", "KEYBINDS", Keys.D0);
            InteriorLightKey = settingsINI.Read<Keys>("InteriorLightKey", "KEYBINDS", Keys.D2);
            ToggleRadioWheelKey = settingsINI.Read<Keys>("ToggleRadioWheelKey", "KEYBINDS", Keys.O);
            ToggleMobileRadioKey = settingsINI.Read<Keys>("ToggleMobileRadioKey", "KEYBINDS", Keys.I);
            ToggleMinimapKey = settingsINI.Read<Keys>("ToggleMinimapKey", "KEYBINDS", Keys.Z);
            OpenDoorKey = settingsINI.Read<Keys>("OpenDoorKey", "KEYBINDS", Keys.Y);
            SeatbeltKey = settingsINI.Read<Keys>("SeatbeltKey", "KEYBINDS", Keys.U);
            ShuffleSeatKey = settingsINI.Read<Keys>("ShuffleSeatKey", "KEYBINDS", Keys.K);
            LeftSignalKey = settingsINI.Read<Keys>("LeftSignalKey", "KEYBINDS", Keys.None);
            RightSignalKey = settingsINI.Read<Keys>("RightSignalKey", "KEYBINDS", Keys.None);
            HazardsKey = settingsINI.Read<Keys>("HazardsKey", "KEYBINDS", Keys.None);
            RedLaserKey = settingsINI.Read<Keys>("RedLaserKey", "KEYBINDS", Keys.OemSemicolon);
            GreenLaserKey = settingsINI.Read<Keys>("GreenLaserKey", "KEYBINDS", Keys.OemQuotes);

        }
        #region MINIMAP FUNCTIONS
        public static void MinimapSafetyCheck(){
            MMsafetyVal = Convert.ToInt32(MMoriginalEnabled) + Convert.ToInt32(MMbigMapEnabled) + Convert.ToInt32(MMzoomoutEnabled) + Convert.ToInt32(MMfullEnabled) + Convert.ToInt32(MMhiddenEnabled);
            if (MMsafetyVal >= 1){
                if (MMoriginalEnabled) { SetMinimapMode(mapStates.original); }
                else if (MMbigMapEnabled) { SetMinimapMode(mapStates.big); }
                else if (MMzoomoutEnabled) { SetMinimapMode(mapStates.zoomout); }
                else if (MMfullEnabled) { SetMinimapMode(mapStates.full); }
                else if (MMhiddenEnabled) { SetMinimapMode(mapStates.hidden); }
            }
        }
        public static void CycleMinimapState(){
            if (mapState == mapStates.hidden) { mapState = mapStates.original; }
            else { mapState++; }
            switch (mapState){
                case mapStates.original:
                    if (MMoriginalEnabled){SetMinimapMode(mapState);}
                    else{CycleMinimapState();}
                    break;
                case mapStates.big:
                    if (MMbigMapEnabled){SetMinimapMode(mapState);}
                    else{CycleMinimapState();}
                    break;
                case mapStates.zoomout:
                    if (MMzoomoutEnabled){SetMinimapMode(mapState);}
                    else{CycleMinimapState();}
                    break;    
                case mapStates.full:
                    if (MMfullEnabled){SetMinimapMode(mapState);}
                    else{CycleMinimapState();}
                    break;
                case mapStates.hidden:
                    if (MMhiddenEnabled){SetMinimapMode(mapState);}
                    else{CycleMinimapState();}
                    break; 
                default:
                    break;
            }
        }
        public static void SetMinimapMode(mapStates mapState){
            switch (mapState){
                case mapStates.original:
                    Function.Call(Hash.SET_BIGMAP_ACTIVE, false, false);
                    Function.Call(Hash.DISPLAY_RADAR, true);
                    break;
                case mapStates.big:
                    Function.Call(Hash.SET_BIGMAP_ACTIVE, true, false);
                    break;
                case mapStates.zoomout:
                    Function.Call(Hash.SET_BIGMAP_ACTIVE, true, false);
                    Function.Call(Hash.SET_RADAR_ZOOM, 6000);
                    break;
                case mapStates.full:
                    Function.Call(Hash.SET_RADAR_ZOOM, 0);
                    Function.Call(Hash.SET_BIGMAP_ACTIVE, true, true);
                    break;
                case mapStates.hidden:
                    Function.Call(Hash.DISPLAY_RADAR, false);
                    break;
                default:
                    break;
            }
        }
        #endregion
        #region CAR FUNCTIONS
        public static bool IsPlayerInThisSeat(int seatIndex){
            if (Function.Call<Ped>(Hash.GET_PED_IN_VEHICLE_SEAT, Game.Player.Character.CurrentVehicle, seatIndex) == Function.Call<Ped>(Hash.GET_PLAYER_PED, Game.Player))//if the ped in the players vehicles x seat = the player ped
            {
                return true;
            }
            return false;}
        public static void ToggleCurrentWindow(Vehicle veh){
            int seatIndex = GetPlayerSeat();
            switch (seatIndex){
                case -2:
                    //Seat Index Return Failure, do nuttin
                    break;
                case -1://driver
                    if (window0down == false){
                        Function.Call(Hash.PLAY_SOUND, -1, "NO", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                        Function.Call(Hash.ROLL_DOWN_WINDOW, veh, 0);
                        window0down = true;
                    }
                    else{
                        Function.Call(Hash.PLAY_SOUND, -1, "YES", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                        Function.Call(Hash.ROLL_UP_WINDOW, veh, 0);
                        window0down = false;
                    }
                    break;
                case 0://passenger
                    if (window1down == false){
                        Function.Call(Hash.PLAY_SOUND, -1, "NO", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                        Function.Call(Hash.ROLL_DOWN_WINDOW, veh, 1);
                        window1down = true;
                    }
                    else{
                        Function.Call(Hash.PLAY_SOUND, -1, "YES", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                        Function.Call(Hash.ROLL_UP_WINDOW, veh, 1);
                        window1down = false;
                    }
                    break;
                case 1://so on
                    if (window2down == false){
                        Function.Call(Hash.PLAY_SOUND, -1, "NO", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                        Function.Call(Hash.ROLL_DOWN_WINDOW, veh, 2);
                        window2down = true;
                    }
                    else{
                        Function.Call(Hash.PLAY_SOUND, -1, "YES", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                        Function.Call(Hash.ROLL_UP_WINDOW, veh, 2);
                        window2down = false;
                    }
                    break;
                case 2://so forth
                    if (window3down == false){
                        Function.Call(Hash.PLAY_SOUND, -1, "NO", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                        Function.Call(Hash.ROLL_DOWN_WINDOW, veh, 3);
                        window3down = true;
                    }
                    else{
                        Function.Call(Hash.PLAY_SOUND, -1, "YES", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                        Function.Call(Hash.ROLL_UP_WINDOW, veh, 3);
                        window3down = false;
                    }
                    break;
            }
        }
        public static void DriverWindowAccess(Vehicle veh, int whichWindow){
            if (IsPlayerInThisSeat(-1)){
                switch (whichWindow){
                    case 1:
                        if (window1down == false){
                            Function.Call(Hash.PLAY_SOUND, -1, "NO", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                            Function.Call(Hash.ROLL_DOWN_WINDOW, veh, 1);
                            window1down = true;
                        }
                        else{
                            Function.Call(Hash.PLAY_SOUND, -1, "YES", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                            Function.Call(Hash.ROLL_UP_WINDOW, veh, 1);
                            window1down = false;
                        }
                        break;
                    case 2:
                        if (window2down == false){
                            Function.Call(Hash.PLAY_SOUND, -1, "NO", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                            Function.Call(Hash.ROLL_DOWN_WINDOW, veh, 2);
                            window2down = true;
                        }
                        else{
                            Function.Call(Hash.PLAY_SOUND, -1, "YES", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                            Function.Call(Hash.ROLL_UP_WINDOW, veh, 2);
                            window2down = false;
                        }
                        break;
                    case 3:
                        if (window3down == false){
                            Function.Call(Hash.PLAY_SOUND, -1, "NO", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                            Function.Call(Hash.ROLL_DOWN_WINDOW, veh, 3);
                            window3down = true;
                        }
                        else{
                            Function.Call(Hash.PLAY_SOUND, -1, "YES", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                            Function.Call(Hash.ROLL_UP_WINDOW, veh, 3);
                            window3down = false;
                        }
                        break;
                }
            }
        }
        public static void ToggleAllWindows(Vehicle veh){
            if (IsPlayerInThisSeat(-1)){
                if (!AWindowDown){
                    Function.Call(Hash.ROLL_DOWN_WINDOWS, veh);
                    window0down = true;
                    window1down = true;
                    window2down = true;
                    window3down = true;
                    Function.Call(Hash.PLAY_SOUND, -1, "NO", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                }
                else{
                    for (int i = 0; i < 8; i++){Function.Call(Hash.ROLL_UP_WINDOW, veh, i);}
                    window0down = false; window1down = false; window2down = false; window3down = false;
                    Function.Call(Hash.PLAY_SOUND, -1, "YES", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                }
                AWindowDown = !AWindowDown;
            }
        }
        public static void ToggleInteriorLight(Vehicle veh, bool tog){
            Function.Call(Hash.SET_VEHICLE_INTERIORLIGHT, veh, tog);
            LightOff = !LightOff;
        }
        public static void ToggleHood(Vehicle veh){
            if (IsPlayerInThisSeat(-1)){
                if (hoodOpen){Function.Call(Hash.SET_VEHICLE_DOOR_SHUT, veh, 4, false);}
                else{Function.Call(Hash.SET_VEHICLE_DOOR_OPEN, veh, 4, false);}
                hoodOpen = !hoodOpen;    
            }
        }
        public static void ToggleTrunk(Vehicle veh){
            if (IsPlayerInThisSeat(-1)){
                if (trunkOpen){Function.Call(Hash.SET_VEHICLE_DOOR_SHUT, veh, 5, false);}
                else{Function.Call(Hash.SET_VEHICLE_DOOR_OPEN, veh, 5, false);}
                trunkOpen = !trunkOpen;    
            }
        }
        public static int GetPlayerSeat(){
            if (IsPlayerInThisSeat(-1)){return -1;}//driver
            else if (IsPlayerInThisSeat(0)){return 0;}//front passenger
            else if (IsPlayerInThisSeat(1)){return 1;}//driver side rear
            else if (IsPlayerInThisSeat(2)){return 2;}//passenger side rear
            else if (IsPlayerInThisSeat(3)){return 3;}//extra seats for compatibility up to the 10 seat Cargobob
            else if (IsPlayerInThisSeat(4)){return 4;}
            else if (IsPlayerInThisSeat(5)){return 5;}
            else if (IsPlayerInThisSeat(6)){return 6;}
            else if (IsPlayerInThisSeat(7)){return 7;}
            else if (IsPlayerInThisSeat(8)){return 8;}
            return -2;
        }
        public static void OpenLocalDoor(Vehicle veh){
            int seatIndex = GetPlayerSeat();
            switch (seatIndex){
                case -2://Seat Index Return Failure, do nuttin
                    break;
                case -1://driver
                    if (door0Open == false){
                        Function.Call(Hash.SET_VEHICLE_DOOR_OPEN, veh, 0, false, false);
                        door0Open = true;
                    }
                    else{
                        Function.Call(Hash.SET_VEHICLE_DOOR_SHUT, veh, 0, false);
                        door0Open = false;
                    }
                    break;
                case 0://passenger
                    if (door1Open == false){
                        Function.Call(Hash.SET_VEHICLE_DOOR_OPEN, veh, 1, false, false);
                        door1Open = true;
                    }
                    else{
                        Function.Call(Hash.SET_VEHICLE_DOOR_SHUT, veh, 1, false);
                        door1Open = false;
                    }
                    break;
                case 1://so on
                    if (door2Open == false){
                        Function.Call(Hash.SET_VEHICLE_DOOR_OPEN, veh, 2, false, false);
                        door2Open = true;
                    }
                    else{
                        Function.Call(Hash.SET_VEHICLE_DOOR_SHUT, veh, 2, false);
                        door2Open = false;
                    }
                    break;
                case 2://so forth
                    if (door3Open == false){
                        Function.Call(Hash.SET_VEHICLE_DOOR_OPEN, veh, 3, false, false);
                        door3Open = true;
                    }
                    else{
                        Function.Call(Hash.SET_VEHICLE_DOOR_SHUT, veh, 3, false);
                        door3Open = false;
                    }
                    break;
            }
        }
        public static void ShuffleToNextSeat(Vehicle veh){
            if (!Function.Call<bool>(Hash.GET_IS_TASK_ACTIVE, Game.Player.Character, 165) && !beltedUp){Function.Call(Hash.TASK_SHUFFLE_TO_NEXT_VEHICLE_SEAT, Game.Player.Character, veh, true);}
        }
        public static void TogglePlayerSeatbelt(Vehicle veh){
            if (!beltedUp){
                beltedUp = true;
                Function.Call(Hash.PLAY_SOUND, -1, "TOGGLE_ON", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                Function.Call(Hash.SET_PED_CAN_BE_KNOCKED_OFF_VEHICLE, veh, 1);
                Function.Call(Hash.SET_PED_CONFIG_FLAG, Game.Player.Character, 32, false);
                Function.Call(Hash.SET_PED_CAN_BE_DRAGGED_OUT, Game.Player.Character, false);
                Notification.Show("Seatbelt On");
            }
            else{
                Function.Call(Hash.PLAY_SOUND, -1, "TOGGLE_ON", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                Function.Call(Hash.SET_PED_CAN_BE_KNOCKED_OFF_VEHICLE, veh, 0);
                Function.Call(Hash.SET_PED_CONFIG_FLAG, Game.Player.Character, 32, true);
                Function.Call(Hash.SET_PED_CAN_BE_DRAGGED_OUT, Game.Player.Character, true);
                //Notification.PostTicker("Seatbelt Off", true); SHVDN4 PREVIEW
                Notification.Show("Seatbelt Off");
                beltedUp = false;
            }
        }
        public static void ToggleTurnSignals(signalTypes signalType, Vehicle veh){
            if (IsPlayerInThisSeat(-1)){
                switch (signalType){
                    case signalTypes.left:
                        if (!hazardsActive){
                            leftSignalActive = !leftSignalActive;
                            Function.Call(Hash.SET_VEHICLE_INDICATOR_LIGHTS, veh, 1, leftSignalActive);
                            if (leftSignalActive) { Function.Call(Hash.SET_VEHICLE_INDICATOR_LIGHTS, veh, 0, false); rightSignalActive = false; }
                        }
                        break;
                    case signalTypes.right:
                        if (!hazardsActive){
                            rightSignalActive = !rightSignalActive;
                            Function.Call(Hash.SET_VEHICLE_INDICATOR_LIGHTS, veh, 0, rightSignalActive);
                            if (rightSignalActive) { Function.Call(Hash.SET_VEHICLE_INDICATOR_LIGHTS, veh, 1, false); leftSignalActive = false; }}
                        break;
                    case signalTypes.hazard:
                        if (!hazardsActive){
                            leftSignalActive = false;
                            rightSignalActive = false;
                            hazardsActive = true;

                        }
                        else{
                            hazardsActive = false;
                        }
                        Function.Call(Hash.SET_VEHICLE_INDICATOR_LIGHTS, veh, 1, hazardsActive);
                        Function.Call(Hash.SET_VEHICLE_INDICATOR_LIGHTS, veh, 0, hazardsActive);
                        break;
                    default:
                        break;
                }
            }
        }
        #endregion
        #region RADIO FUNCTIONS
        public static void TogglePlayerMobileRadio(){
            if (Game.Player.Character.CurrentVehicle == null){
                if (mobileRadio){
                    Function.Call(Hash.SET_MOBILE_PHONE_RADIO_STATE, false);
                    Function.Call(Hash.SET_AUDIO_FLAG, "MobileRadioInGame", 0);
                    Function.Call(Hash.SET_AUDIO_FLAG, "AllowRadioDuringSwitch", 0);
                    Notification.Show("Mobile Radio Off");
                    mobileRadio = false;
                    radioWheelDisabled = false;
                }
                else{
                    Function.Call(Hash.SET_MOBILE_PHONE_RADIO_STATE, true);
                    Function.Call(Hash.SET_AUDIO_FLAG, "MobileRadioInGame", 1);
                    Function.Call(Hash.SET_AUDIO_FLAG, "AllowRadioDuringSwitch", 1);
                    Notification.Show("Mobile Radio On");
                    mobileRadio = true;
                }
            }
        }
        public static void ToggleRadioWheel(){
            if (radioWheelDisabled){
                radioWheelDisabled = false;
                Notification.Show("Radio Wheel Enabled");
            }
            else{
                radioWheelDisabled = true;
                Notification.Show("Radio Wheel Disabled");
            }
        }
        #endregion
        #region LASER FUNCTIONS
        public static void ToggleLaser(bool laserColor)
        {
            if (laserColor)//green
            {
                Weapon weapon = Game.Player.Character.Weapons.Current;
                //Notification.Show(weapon.Model.ToString());
                weapon.Components[laserhash].Active = !weapon.Components[laserhash].Active;
                weapon.Components[laserhashREH].Active = !weapon.Components[laserhashREH].Active;
                weapon.Components[PIlaserhash].Active = !weapon.Components[PIlaserhash].Active;
            }
            else//red
            {
                Weapon weapon = Game.Player.Character.Weapons.Current;
                //Notification.Show(weapon.Model.ToString());
                weapon.Components[redlaserhash].Active = !weapon.Components[redlaserhash].Active;
                weapon.Components[redlaserhashREH].Active = !weapon.Components[redlaserhashREH].Active;
                weapon.Components[PIlaserhashIR].Active = !weapon.Components[PIlaserhashIR].Active;
            }
        }
        #endregion
    }
}