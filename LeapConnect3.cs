/*
 * LeapConnect3
 * Adam O'Grady
 * Copyright MIT License 2014
 * This version of LeapConnect implements the first private beta of the Skeletal Tracking API
 */

using UnityEngine;
using System.Collections;
using Leap;

public class LeapConnect3 : MonoBehaviour {
	
	//Public Variables
	public GameObject m_palm_template; //The template object to use for the palm
	public GameObject m_joint_template; //The template object to use for joints
	public GameObject m_segment_template; //The template object to use for segments between joints
	public static Frame m_frame	= null; //Frame object for access to all leap data in the frame
	public static Frame last_frame = null; //Frame object holding all data for previous frame
	public static HandStruct left_hand; //Structure to hold left hand data, static for easy external access
	public static HandStruct right_hand; //Structure to hold right hand data, static for easy external access
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
		left_hand.hand_obj = null; //Set palm gameobject reference to null
		left_hand.hand_upd = null; //Set hand Leap frame information reference to null
		right_hand.hand_obj = null; //Set palm gameobject reference to null
		right_hand.hand_upd = null; //Set hand Leap frame information reference to null
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
			if (h.IsLeft) { //If the hand is a left hand...
				if (left_hand.hand_upd == null) { //If the Leap frame hand info reference is currently null...
					left_hand.hand_upd = h; //The Leap frame hand info reference is stored
					left_hand.hand_obj = Instantiate(m_palm_template) as GameObject; //Instantiate a copy of the palm template and assign to hand object
					left_hand.hand_obj.transform.parent = gameObject.transform; //The object this script is attached to is now the parent of the hand object
					left_hand.fingers = new FingerStruct[5]; //Create collection of fingers
					for (int i = 0; i < 5; i++) { //For each of the fingers...
						left_hand.fingers[i].joints = new GameObject[4]; //Create collection of joints
						left_hand.fingers[i].segments = new GameObject[3]; //Create collection of segments
						for (int j = 0; j < 4; j++) { //For each of the joints and segments...
							left_hand.fingers[i].joints[j] = Instantiate(m_joint_template) as GameObject; //Instantiate a copy of the joint template and assign to joint object
							left_hand.fingers[i].joints[j].transform.parent = gameObject.transform; //The object this script is attached to is now the parent of the joint object
							if (j < 3) { //One less segment than joints...
								left_hand.fingers[i].segments[j] = Instantiate(m_segment_template) as GameObject; //Instantiate a copy of the segment template and assign to segment object
								left_hand.fingers[i].segments[j].transform.parent = gameObject.transform; //The object this script is attached to is now the parent of the segment object
							}
						}
					}
				}
			}
			if (h.IsRight) { //If the hand is a right hand...
				if (right_hand.hand_upd == null) { //If the Leap frame hand info reference is currently null...
					right_hand.hand_upd = h; //The Leap frame hand info reference is stored
					right_hand.hand_obj = Instantiate(m_palm_template) as GameObject; //Instantiate a copy of the palm template and assign to hand object
					right_hand.hand_obj.transform.parent = gameObject.transform; //The object this script is attached to is now the parent of the hand object
					right_hand.fingers = new FingerStruct[5]; //Create collection of fingers
					for (int i = 0; i < 5; i++) { //For each of the fingers...
						right_hand.fingers[i].joints = new GameObject[4]; //Create collection of joints
						right_hand.fingers[i].segments = new GameObject[3]; //Create collection of segments
						for (int j = 0; j < 4; j++) { //For each of the joints and segments...
							right_hand.fingers[i].joints[j] = Instantiate(m_joint_template) as GameObject; //Instantiate a copy of the joint template and assign to joint object
							right_hand.fingers[i].joints[j].transform.parent = gameObject.transform; //The object this script is attached to is now the parent of the joint object
							if (j < 3) { //One less segment than joints...
								right_hand.fingers[i].segments[j] = Instantiate(m_segment_template) as GameObject; //Instantiate a copy of the segment template and assign to segment object
								right_hand.fingers[i].segments[j].transform.parent = gameObject.transform; //The object this script is attached to is now the parent of the segment object
							}
						}
					}
				}
			}
		}
	}
	
	//Update hands that are in view
	private void UpdateHands() {
		if (left_hand.hand_obj != null) { //If left palm GameObject is not null (ie, is instantiated)...
			foreach (Hand h in m_frame.Hands) { //For each hand in the current Leap frame...
				if (left_hand.hand_upd.Id == h.Id) { //If Leap reference hand ID matches current Leap hand ID...
					left_hand.hand_upd = h; //Leap reference hand info is now equal to current Leap hand info
					foreach (Finger f in h.Fingers) { //For each of the fingers on the currently examined Leap frame hand...
						for (int i = 0; i < 4; i++) { //For each of the joints and segments of the finger...
							Vector normalisedFinger = normalise_box.NormalizePoint(f.JointPosition((Finger.FingerJoint)i)); //Find Leap normalised Vector (through interaction box) of current joint
							left_hand.fingers[(int)f.Type()].joints[i].transform.localPosition = new Vector3((normalisedFinger.x * 10) - 5, (normalisedFinger.y * 10) - 5, (-1 * normalisedFinger.z * 10) + 15); //Set position of joint in relation to GameObject this script is attached to
							if (i < 3) { //One less segment than joints...
								left_hand.fingers[(int)f.Type()].segments[i].transform.localPosition = Vector3.Lerp(left_hand.fingers[(int)f.Type()].joints[i].transform.localPosition, left_hand.fingers[(int)f.Type()].joints[i + 1].transform.localPosition, 0.5F); //Set the segment position to halfway between current and next joint
								Vector3 temp = (left_hand.fingers[(int)f.Type()].joints[i + 1].transform.position - left_hand.fingers[(int)f.Type()].segments[i].transform.position).normalized; //Subtract segment position from next joint, normalise resulting temporary vector
								left_hand.fingers[(int)f.Type()].segments[i].transform.rotation = Quaternion.LookRotation(temp); //Set rotation of segment to face direction of temporary vector (pointing it at the next joint)
							}
						}
					}
				}
			}
			Vector normalisedHand = normalise_box.NormalizePoint(left_hand.hand_upd.PalmPosition, true); //Normalise (in Leap interaction box) the palm position
			left_hand.hand_obj.transform.localPosition = new Vector3((normalisedHand.x * 10) - 5, (normalisedHand.y * 10) - 5, (-1 * normalisedHand.z * 10) + 15); //Set position of palm in relation to GameObject this script is attached to
			left_hand.hand_obj.transform.eulerAngles = new Vector3(-1 * (180 / Mathf.PI * left_hand.hand_upd.Direction.Pitch), 180 / Mathf.PI * left_hand.hand_upd.Direction.Yaw, 180 / Mathf.PI * left_hand.hand_upd.PalmNormal.Roll); //Rotate the palm to face the correct direction
		}
		if (right_hand.hand_obj != null) { //If right palm GameObject is not null (ie, is instantiated)...
			foreach (Hand h in m_frame.Hands) { //For each hand in the current Leap frame...
				if (right_hand.hand_upd.Id == h.Id) { //If Leap reference hand ID matches current Leap hand ID...
					right_hand.hand_upd = h; //Leap reference hand info is now equal to current Leap hand info
					foreach (Finger f in h.Fingers) { //For each of the fingers on the currently examined Leap frame hand...
						for (int i = 0; i < 4; i++) { //For each of the joints and segments of the finger...
							Vector normalisedFinger = normalise_box.NormalizePoint(f.JointPosition((Finger.FingerJoint)i)); //Find Leap normalised Vector (through interaction box) of current joint
							right_hand.fingers[(int)f.Type()].joints[i].transform.localPosition = new Vector3((normalisedFinger.x * 10) - 5, (normalisedFinger.y * 10) - 5, (-1 * normalisedFinger.z * 10) + 15); //Set position of joint in relation to GameObject this script is attached to
							if (i < 3) { //One less segment than joints...
								right_hand.fingers[(int)f.Type()].segments[i].transform.localPosition = Vector3.Lerp(right_hand.fingers[(int)f.Type()].joints[i].transform.localPosition, right_hand.fingers[(int)f.Type()].joints[i + 1].transform.localPosition, 0.5F); //Set the segment position to halfway between current and next joint
								Vector3 temp = (right_hand.fingers[(int)f.Type()].joints[i + 1].transform.position - right_hand.fingers[(int)f.Type()].segments[i].transform.position).normalized; //Subtract segment position from next joint, normalise resulting temporary vector
								right_hand.fingers[(int)f.Type()].segments[i].transform.rotation = Quaternion.LookRotation(temp); //Set rotation of segment to face direction of temporary vector (pointing it at the next joint)
							}
						}
					}
				}
			}
			Vector normalisedHand = normalise_box.NormalizePoint(right_hand.hand_upd.PalmPosition, true); //Normalise (in Leap interaction box) the palm position
			right_hand.hand_obj.transform.localPosition = new Vector3((normalisedHand.x * 10) - 5, (normalisedHand.y * 10) - 5, (-1 * normalisedHand.z * 10) + 15); //Set position of palm in relation to GameObject this script is attached to
			right_hand.hand_obj.transform.eulerAngles = new Vector3(-1 * (180 / Mathf.PI * right_hand.hand_upd.Direction.Pitch), 180 / Mathf.PI * right_hand.hand_upd.Direction.Yaw, 180 / Mathf.PI * right_hand.hand_upd.PalmNormal.Roll); //Rotate the palm to face the correct direction
		}
	}
	
	//Remove unnecessary hands as they disappear from view
	private void RemoveHands() {
		bool present = false; //Set the presence of the (left) hand to false initially
		if (left_hand.hand_obj != null) { //If the left hand GameObject isn't null...
			foreach (Hand h in m_frame.Hands) { //For each hand in the current frame...
				if (left_hand.hand_upd.Id == h.Id) { //If a Leap reference hand ID matches a Leap hand ID in the current frame...
					present = true; //Set presence of (left) hand to true
				}
			}
			if (!present) { //If the (left) hand isn't present...
				GameObject.Destroy(left_hand.hand_obj); //Destroy the GameObject representing the palm
				left_hand.hand_obj = null; //Set the GameObject reference to null
				for (int i = 0; i < 5; i++) { //For each finger...
					for (int j = 0; j < 4; j++) { //For each segment and joint...
						GameObject.Destroy(left_hand.fingers[i].joints[j]); //Destroy the GameObject representing the joint
						left_hand.fingers[i].joints[j] = null; //Set the GameObject reference to null
						if (j < 3) { //One less segment than joints...
							GameObject.Destroy(left_hand.fingers[i].segments[j]);  //Destroy the GameObject representing the segment
							left_hand.fingers[i].segments[j] = null; //Set the GameObject reference to null
						}
					}
				}
				left_hand.hand_upd = null; //Set the Leap hand reference to null
			}
		}
		present = false; //Set the presence of the (right) hand to false initially
		if (right_hand.hand_obj != null) { //If the right hand GameObject isn't null...
			foreach (Hand h in m_frame.Hands) { //For each hand in the current frame...
				if (right_hand.hand_upd.Id == h.Id) { //If a Leap reference hand ID matches a Leap hand ID in the current frame...
					present = true; //Set presence of (right) hand to true
				}
			}
			if (!present) { //If the (right) hand isn't present...
				GameObject.Destroy(right_hand.hand_obj); //Destroy the GameObject representing the palm
				right_hand.hand_obj = null; //Set the GameObject reference to null
				for (int i = 0; i < 5; i++) { //For each finger...
					for (int j = 0; j < 4; j++) { //For each segment and joint...
						GameObject.Destroy(right_hand.fingers[i].joints[j]); //Destroy the GameObject representing the joint
						right_hand.fingers[i].joints[j] = null; //Set the GameObject reference to null
						if (j < 3) { //One less segment than joints...
							GameObject.Destroy(right_hand.fingers[i].segments[j]); //Destroy the GameObject representing the segment
							right_hand.fingers[i].segments[j] = null; //Set the GameObject reference to null
						}
					}
				}
				right_hand.hand_upd = null; //Set the Leap hand reference to null
			}
		}
	}
}
