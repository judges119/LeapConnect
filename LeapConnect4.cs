/*
 * LeapConnect4
 * Adam O'Grady
 * Copyright MIT license 2014
 * This version of LeapConnect implements the public beta of the Skeletal Tracking API and uses a hands array (the length of which can be set by the developer to constrain or free).
 * Currently does not use Leap.Bone.Center() function as it was not available in the SDK at the time.
 */

using UnityEngine;
using System.Collections;
using Leap;

public class LeapConnect4 : MonoBehaviour {

	//Constants
	public const int NHANDS = 2; //Number of hands variable

	//Public Variables
	public GameObject m_palm_template; //The template object to use for the palm
	public GameObject m_joint_template; //The template object to use for joints
	public GameObject m_segment_template; //The template object to use for segments between joints
	public static Frame m_frame	= null; //Frame object for access to all leap data in the frame
	public static Frame last_frame = null; //Frame object holding all data for previous frame
	public static HandStruct[] hands = new HandStruct[NHANDS]; //Structure to hold hand data, static for easy external access
	public static InteractionBox normalise_box; //Current interaction box for this frame

	public struct HandStruct { //Struct to store hand data
		public Hand hand_upd; //The Leap information from the current frame about the hand
		public GameObject hand_obj; //The GameObject for the palm
		public FingerStruct[] fingers; //The collection of fingers for the current hand
	}
	
	public struct FingerStruct { //Struct to store finger data
		public GameObject[] joints; //Collection of GameObjects representing joints of the finger (including the tip)
		public GameObject[] segments; //Collection of GameObjects representing the finger segments between the joints
	}
	
	//Private Variables
	static Leap.Controller m_controller = new Leap.Controller(); //Leap Controller object
	
	//Catch latest frame
	public static Leap.Frame Frame {
		get { return m_frame; }
	}
	
	//Use this for initialization
	void Start() {
		for (int i = 0; i < NHANDS; i++) { //For each hand in hands array...
			hands[i].hand_obj = null; //Set palm gameobject reference to null
			hands[i].hand_upd = null; //Set hand Leap frame information reference to null
		}
	}
	
	//Update is called once per frame
	void Update() {
		if (m_controller != null) { //If the controller is not null...
			last_frame = (m_frame == null) ? Frame.Invalid : m_frame; //If the existing frame is null, set last_frame to an invalid frame, otherwise set it to the current (now old) frame
			m_frame	= m_controller.Frame(); //Set the current frame to the latest frame from the controller
		}
		normalise_box = m_frame.InteractionBox; //Set the interaction box to the current frame's interaction box
		AddHands(); //Add any new hands
		UpdateHands(); //Update any existing hands
		RemoveHands(); //Remove any old hands
	}
	
	//Add hands, up to a maximum of three (2 real, one accidental)
	private void AddHands() {
		foreach (Hand h in m_frame.Hands) { //For each hand in Leap frame...
			bool handKnown = false; //Boolean for if current hand (h) is known
			foreach (HandStruct g in hands) { //For each hand in the hands array...
				if (g.hand_upd != null) { //If hand array item is not null...
					if (h.Id == g.hand_upd.Id) { //If hand ID is same as hand array item ID...
						handKnown = true; //This hand is known
					}
				}
			}
			if (handKnown) { //If this hand is known...
				continue; //Continue with next hand
			}
			int x = 0;
			while (hands[x].hand_upd != null) { //Iterate past all known hands until you find array entry that is null
				x++;
			}
			hands[x].hand_upd = h; //Store Leap hand data for this frame in hand array
			hands[x].hand_obj = Instantiate(m_palm_template) as GameObject; //Instantiate a copy of the palm template and assign to hand object
			hands[x].hand_obj.transform.parent = gameObject.transform; //The object this script is attached to is now the parent of the hand object
			hands[x].fingers = new FingerStruct[5]; //Create collection of fingers
			for (int i = 0; i < 5; i++) { //For each of the fingers...
				hands[x].fingers[i].joints = new GameObject[4]; //Create collection of joints
				hands[x].fingers[i].segments = new GameObject[3]; //Create collection of segments
				for (int j = 0; j < 4; j++) { //For each of the joints and segments...
					hands[x].fingers[i].joints[j] = Instantiate(m_joint_template) as GameObject; //Instantiate a copy of the joint template and assign to joint object
					hands[x].fingers[i].joints[j].transform.parent = gameObject.transform; //The object this script is attached to is now the parent of the joint object
					if (j < 3) { //One less segment than joints...
						hands[x].fingers[i].segments[j] = Instantiate(m_segment_template) as GameObject; //Instantiate a copy of the segment template and assign to segment object
						hands[x].fingers[i].segments[j].transform.parent = gameObject.transform; //The object this script is attached to is now the parent of the segment object
					}
				}
			}
		}
	}
	
	//Update hands that are in view
	private void UpdateHands() {
		for (int x = 0; x < NHANDS; x++) { //For each hand in the hands array
			if (hands[x].hand_upd != null) { //If the hand array item is not null (ie; is present)...
				foreach (Hand h in m_frame.Hands) { //For each hand in the current Leap frame...
					if (hands[x].hand_upd.Id == h.Id) { //If Leap reference hand ID matches current Leap hand ID...
						hands[x].hand_upd = h; //Leap reference hand info is now equal to current Leap hand info
						foreach (Finger f in h.Fingers) { //For each of the fingers on the currently examined Leap frame hand...
							for (int i = 0; i < 4; i++) { //For each of the joints and segments of the finger...
								Vector normalisedFinger = normalise_box.NormalizePoint(f.JointPosition((Finger.FingerJoint)i)); //Find Leap normalised Vector (through interaction box) of current joint
								hands[x].fingers[(int)f.Type()].joints[i].transform.localPosition = new Vector3((normalisedFinger.x * 10) - 5, (normalisedFinger.y * 10) - 5, (-1 * normalisedFinger.z * 10) + 15); //Set position of joint in relation to GameObject this script is attached to
								if (i < 3) { //One less segment than joints...
									hands[x].fingers[(int)f.Type()].segments[i].transform.localPosition = Vector3.Lerp(hands[x].fingers[(int)f.Type()].joints[i].transform.localPosition, hands[x].fingers[(int)f.Type()].joints[i + 1].transform.localPosition, 0.5F); //Set the segment position to halfway between current and next joint
									Vector3 temp = (hands[x].fingers[(int)f.Type()].joints[i + 1].transform.position - hands[x].fingers[(int)f.Type()].segments[i].transform.position).normalized; //Subtract segment position from next joint, normalise resulting temporary vector
									hands[x].fingers[(int)f.Type()].segments[i].transform.rotation = Quaternion.LookRotation(temp); //Set rotation of segment to face direction of temporary vector (pointing it at the next joint)
								}
							}
						}
					}
				}
				Vector normalisedHand = normalise_box.NormalizePoint(hands[x].hand_upd.PalmPosition, true); //Normalise (in Leap interaction box) the palm position
				hands[x].hand_obj.transform.localPosition = new Vector3((normalisedHand.x * 10) - 5, (normalisedHand.y * 10) - 5, (-1 * normalisedHand.z * 10) + 15); //Set position of palm in relation to GameObject this script is attached to
				hands[x].hand_obj.transform.eulerAngles = new Vector3(-1 * (180 / Mathf.PI * hands[x].hand_upd.Direction.Pitch), 180 / Mathf.PI * hands[x].hand_upd.Direction.Yaw, 180 / Mathf.PI * hands[x].hand_upd.PalmNormal.Roll); //Rotate the palm to face the correct direction
			}
		}
	}
	
	//Remove unnecessary hands as they disappear from view
	private void RemoveHands() {
		for (int x = 0; x < NHANDS; x++) { //For each
			bool present = false; //Set the presence of the hand to false initially
			if (hands[x].hand_upd != null) { //If the hand array item is not null (ie; is present)...
				foreach (Hand h in m_frame.Hands) { //For each hand in the current frame...
					if (hands[x].hand_upd.Id == h.Id) { //If a Leap reference hand ID (in hands array) matches a Leap hand ID in the current frame...
						present = true; //Set presence of (left) hand to true
						break; //Quit iterating through loop
					}
				}
				if (!present) { //If the hand isn't present...
					GameObject.Destroy(hands[x].hand_obj); //Destroy the GameObject representing the palm
					hands[x].hand_obj = null; //Set the GameObject reference to null
					for (int i = 0; i < 5; i++) { //For each finger...
						for (int j = 0; j < 4; j++) { //For each segment and joint...
							GameObject.Destroy(hands[x].fingers[i].joints[j]); //Destroy the GameObject representing the joint
							hands[x].fingers[i].joints[j] = null; //Set the GameObject reference to null
							if (j < 3) { //One less segment than joints...
								GameObject.Destroy(hands[x].fingers[i].segments[j]);  //Destroy the GameObject representing the segment
								hands[x].fingers[i].segments[j] = null; //Set the GameObject reference to null
							}
						}
					}
					hands[x].hand_upd = null; //Set the Leap hand reference to null
				}
			}
		}
	}
}
