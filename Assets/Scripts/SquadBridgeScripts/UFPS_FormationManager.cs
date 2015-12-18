using UnityEngine;
using System.Collections;
using RAIN.Core;
using UnityEngine.UI;
//Thanks to Johannes Holweg from Germany for creating this script to be used as an interface with UFPS
//NOTES - Make sure the UFPS player has an AI\Scripts\FireTeamFormationHarness added
//Extended to include more by RedHawk for Squad Commands (such as Form Up and Attack)

namespace red{//Create any namespace you want.  Just for organizing
    public class UFPS_FormationManager : MonoBehaviour {

        FormationHarnessElement myFormationHarnessElement;
        [Header("Input Key Commands for Formation Control")]
        public KeyCode wedge = KeyCode.F1;
        public KeyCode column = KeyCode.F2;
        public KeyCode skirmishLeft = KeyCode.F3;
        public KeyCode skirmishRight = KeyCode.F4;
        public KeyCode echelonLeft = KeyCode.F5;
        public KeyCode echelonRight = KeyCode.F6;
        [Header("Input Key Commands for Squad Orders")]
        public KeyCode formUp = KeyCode.F7;
        public KeyCode attack = KeyCode.F8;
        public KeyCode coverMe = KeyCode.F9;
        public KeyCode takeCover = KeyCode.F10;
        public KeyCode flank = KeyCode.F11;
        public Text displayText;

        private AIRig myAI;
        // Use this for initialization
        void Start () {
            myAI = this.GetComponentInChildren<AIRig>();
            if(myAI==null)
                Debug.Log("You need to add an AI Component!",this.gameObject);
            myFormationHarnessElement = myAI.AI.GetCustomElement<FormationHarnessElement>();
            if(displayText!=null)
                displayText.text=
                    "FORMATION TYPE:\n"+
                    wedge+" - Wedge Formation\n"+
                    column+" - Column Formation\n"+
                    skirmishLeft+" - Skirmish Left Formation\n"+
                    skirmishRight+" - Skirmish Right Formation\n"+
                    echelonLeft+" - Echelon Left Formation\n"+
                    echelonRight+" - Echelon Right Formation\n"+
                    "\n"+
                    "SQUAD COMMANDS\n"+
                    formUp+" - Squad Form Up\n"+
                    attack+" - Squad Attack\n"+
                    coverMe+" - Squad Cover Me\n"+
                    takeCover+" - Squad Take Cover\n"+
                    flank+" - Squad Flank\n";
        }
        
        // Update is called once per frame
        void Update () {
            if(Input.GetKey(wedge)){
                if(myFormationHarnessElement!=null)
                    myFormationHarnessElement.FormationMode = "wedge";
            }
            if(Input.GetKey(column)){
                if(myFormationHarnessElement!=null)
                    myFormationHarnessElement.FormationMode = "column";
            }
            if(Input.GetKey(skirmishLeft)){
                if(myFormationHarnessElement!=null)
                    myFormationHarnessElement.FormationMode = "skirmish left";
            }
            if(Input.GetKey(skirmishRight)){
                if(myFormationHarnessElement!=null)
                    myFormationHarnessElement.FormationMode = "skirmish right";
            }
            if(Input.GetKey(echelonLeft)){
                if(myFormationHarnessElement!=null)
                    myFormationHarnessElement.FormationMode = "echelon left";
            }
            if(Input.GetKey(echelonRight)){
                if(myFormationHarnessElement!=null)
                    myFormationHarnessElement.FormationMode = "echelon right";
            }
            if(Input.GetKey(formUp)){
                SendCommand("form up");
            }
            if(Input.GetKey(attack)){
                SendCommand("attack");
            }
            if(Input.GetKey(coverMe)){
                SendCommand("cover me");
            }
            if(Input.GetKey(takeCover)){
                SendCommand("take cover");
            }
            if(Input.GetKey(flank)){
                SendCommand("flank");
            }
        }
        //BELOW IS STRAIGHT FROM THE PlayerInputElement script included with Squad Command
        /// <summary>
        /// Send a command to your squad through the communication system.  This also sets last command
        /// for displaying on the UI
        /// </summary>
        /// <param name="aCommand">The command to send</param>
        private void SendCommand(string aCommand)
        {
            string tChannel = myAI.AI.WorkingMemory.GetItem<string>("teamComm");
        //    string tChannel = AI.WorkingMemory.GetItem<string>("teamComm");
            CommunicationManager.Instance.Broadcast(tChannel, "command", aCommand);

        }

    }
}