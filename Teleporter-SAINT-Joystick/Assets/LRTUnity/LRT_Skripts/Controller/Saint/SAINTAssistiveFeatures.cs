
// Feature 1: Trajectory visualization (-> SAINTPoseArraySubscriber)
// Feature 2: Cursor position projection
// Feature 3: Grasping and dropdown assistance
// Feature 4: Box collision warning
// Feature 5: Workspace restriction
// Feature 6: Z-level assistance

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SAINTAssistiveFeatures : MonoBehaviour
{

	public SAINTState saintState;
	public GameObject uiOperatorPosition;
	private OperatorArmState operatorArmState;

	// Activate Features
	bool CursorPositionProjection = true;
	bool GraspDropdownFeedback = true;
	bool WorkspaceRestrictions = true;
	bool CollisionWarning = true;
	bool ZLevelAssistance = true;

	// Workspace Parameters
	bool isItem = true;
	bool isBox = true;
	// if isBox == true:
	float boxPose = 0.1f;
	float boxposey = (0.262f - 0.135f)/2;	// get from parameter-server when available
	float boxHeight = 0.77f/2;				// get from parameter-server when available
	float groundLevel = -0.152f; 			// get from parameter-server when available
	
	float operatorHeight = 0.1f;				// adjust for real robot
	float itemSize = 0.1f;					// adjust for real item

	// ** CursorPositionProjection **
	public Text controlState;			// ControlState
	public GameObject uiCursorFeedback;	// GameObject CursorFeedback
	public GameObject robotPosition;	// link7

	// *** Grasp/DropdownFeedback ***
	public GameObject graspingFeedback;	// GameObject UIGraspingFeedback
	public GameObject dropdownFeedback;	// GameObject UIDropdownFeedback
	// Parameters for Grasp/DropdownFeedback
	float distanceGraspDropdownFeedbackStart = 0.2f;

	// ****** CollisionWarning ******
	public GameObject collisionWarning;	// GameObject UICollisionWarning
	// Parameters for CollisionWarning		
	float buffer = 0.09f;

	// **** WorkspaceRestriction ****
	public GameObject restrictionFront;	// GameObject UIRestrictionFront
	public GameObject restrictionBack;	// GameObject UIRestrictionBack
	public GameObject restrictionRight;	// GameObject UIRestrictionRight
	public GameObject restrictionLeft;	// GameObject UIRestrictionLeft
	// Parameters for WorkspaceRestrictionFeedback
	float distanceRestricitionFeedbackStart = 0.2f;
	float positionRestrictionFront = 0.82f;
	float positionRestrictionBack = -0.67f;
	float positionRestrictionRight = 1.005f;
	float positionRestrictionLeft = -0.485f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

	// Get cursper position
	Vector3 cur_pos = uiOperatorPosition.transform.position;


	// ******************************
	// ** CursorPositionProjection **
	// ******************************

	Vector3 cursor_feedback_pos;
	cursor_feedback_pos.y = groundLevel + 0.02f;

	controlState.text = saintState.ControlState.ToString();

	if (controlState.text == "MANUAL")
	{
		cursor_feedback_pos.z = cur_pos.z;
		cursor_feedback_pos.x = cur_pos.x;
	}
	else
	{
		cursor_feedback_pos.z = robotPosition.transform.position.z;
		cursor_feedback_pos.x = robotPosition.transform.position.x;
	}
	// Set position
	uiCursorFeedback.transform.position = cursor_feedback_pos;
	// Set color
	uiCursorFeedback.GetComponent<Renderer>().material.color = new Color(0.0f, 1.0f, 1.0f, 1.0f);
	// Activate
	if (CursorPositionProjection == false) 
		{
			uiCursorFeedback.SetActive(false);
		} else 
		{
			uiCursorFeedback.SetActive(true);
		}

	// ******************************
	// ** CursorPositionProjection **
	// ******************************


	// ******************************
	// *** Grasp/DropdownFeedback ***
	// ******************************

	// ****** GraspingFeedback ******

	Vector3 scalegrasp = graspingFeedback.transform.localScale;

	// Get position and size of GraspingArea
	float xgrasp = graspingFeedback.transform.position.x;
	float zgrasp = graspingFeedback.transform.position.z;
	float scalegraspx = graspingFeedback.transform.localScale.x;
	float scalegraspz = graspingFeedback.transform.localScale.z;
	
	float scalesmallgrasp;
	float scalebiggrasp;
	if (scalegraspx < scalegraspz) 
		{
			scalesmallgrasp = scalegraspx; scalebiggrasp = scalegraspz;
		} else 
		{
			scalesmallgrasp = scalegraspz; scalebiggrasp = scalegraspx;
		}
	float diagGrasp = Mathf.Sqrt(scalesmallgrasp/2*scalesmallgrasp/2+scalebiggrasp/2*scalebiggrasp/2);

	// Current distance to GraspingArea
	float distanceGrasp = Mathf.Sqrt((cur_pos.x-xgrasp)*(cur_pos.x-xgrasp)+(cur_pos.z-zgrasp)*(cur_pos.z-zgrasp));

	// FeedbackRadius
	float rgrasp = diagGrasp + distanceGraspDropdownFeedbackStart;

	// Somewhere
	if (distanceGrasp > rgrasp) 
		{
			graspingFeedback.SetActive(false);
		}
	// GraspingArea
	else if (Convert.ToSingle(cur_pos.x) < 0.01 && Convert.ToSingle(cur_pos.x) > -0.2 && Convert.ToSingle(cur_pos.z) > 0.45 && Convert.ToSingle(cur_pos.z) < 0.84)
	{
		graspingFeedback.SetActive(GraspDropdownFeedback);
		graspingFeedback.GetComponent<MeshRenderer>().material.color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
	}
	// Within FeedbackRadius
	else
	{
		graspingFeedback.SetActive(GraspDropdownFeedback);
		// FeedbackColor
		float rotGrasp = 0.5f/(rgrasp-scalesmallgrasp/2) * distanceGrasp + 1 - 0.5f*rgrasp/(rgrasp-scalesmallgrasp/2);
		// FeedbackTransparency
		float alphagrasp = 0.7f/(scalesmallgrasp/2-rgrasp) * distanceGrasp - 0.7f/(scalesmallgrasp/2-rgrasp)*rgrasp;
		// Apply changes
		graspingFeedback.GetComponent<Renderer>().material.color = new Color(rotGrasp, 1.0f, 0.0f, alphagrasp);
	}

	// Feedback Scale
	if (distanceGrasp < diagGrasp) 
		{
			scalegrasp.y = 0.0005f;
		}
	else 
		{
			scalegrasp.y = (1.0f-0.0005f)/(rgrasp-diagGrasp) * distanceGrasp + 1.0f - (1.0f-0.0005f)*rgrasp/(rgrasp-diagGrasp);
		}
	//Apply changes
	graspingFeedback.transform.localScale = scalegrasp;

	// ****** DropdownFeedback ******

	Vector3 scaledd = dropdownFeedback.transform.localScale;

	// Get position and size of DropdownArea
	float xdd = dropdownFeedback.transform.position.x;
	float zdd = dropdownFeedback.transform.position.z;
	float scaleddx = dropdownFeedback.transform.localScale.x;
	float scaleddz = dropdownFeedback.transform.localScale.z;
	
	float scalesmalldd;
	float scalebigdd;
	if (scaleddx < scaleddz) 
		{
			scalesmalldd = scaleddx; scalebigdd = scaleddz;
		} else 
		{
			scalesmalldd = scaleddz; scalebigdd = scaleddx;
		}
	float diagdd = Mathf.Sqrt(scalesmalldd/2*scalesmalldd/2+scalebigdd/2*scalebigdd/2);

	// Current distance to DropdownArea
	float distancedd = Mathf.Sqrt((cur_pos.x-xdd)*(cur_pos.x-xdd)+(cur_pos.z-zdd)*(cur_pos.z-zdd));

	// FeedbackRadius
	float rdd = diagdd + distanceGraspDropdownFeedbackStart;

	// Somewhere
	if (distancedd > rdd) 
		{
			dropdownFeedback.SetActive(false);
		}
	// DropdownArea
	else if (Convert.ToSingle(cur_pos.x) < 0.61 && Convert.ToSingle(cur_pos.x) > 0.47 && Convert.ToSingle(cur_pos.z) > -0.1 && Convert.ToSingle(cur_pos.z) < 0.11)
	{
		dropdownFeedback.SetActive(GraspDropdownFeedback);
		dropdownFeedback.GetComponent<MeshRenderer>().material.color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
	}
	// Within FeedbackRadius
	else
	{
		dropdownFeedback.SetActive(GraspDropdownFeedback);
		// FeedbackColor
		float rotdd = 0.5f/(rdd-scalesmalldd/2) * distancedd + 1 - 0.5f*rdd/(rdd-scalesmalldd/2);
		// FeedbackTransparency
		float alphadd = 0.7f/(scalesmalldd/2-rdd) * distancedd - 0.7f/(scalesmalldd/2-rdd)*rdd;
		// Apply changes
		dropdownFeedback.GetComponent<Renderer>().material.color = new Color(rotdd, 1.0f, 0.0f, alphadd);
	}

	// Feedback Scale
	if (distancedd < diagdd) 
		{
			scaledd.y = 0.0005f;
		}	else 
		{
			scaledd.y = (1.0f-0.0005f)/(rdd-diagdd) * distancedd + 1.0f - (1.0f-0.0005f)*rdd/(rdd-diagdd);
		}
	//Apply changes
	dropdownFeedback.transform.localScale = scaledd;

	// ******************************
	// *** Grasp/DropdownFeedback ***
	// ******************************


	// ******************************
	// ***** BoxCollisionWarning ****
	// ******************************
	
	if (isBox)
	{
		Vector3 scalecol = collisionWarning.transform.localScale;

		float boxtop = boxposey + boxHeight/2;			// position
		float boxbottom = boxposey - boxHeight/2 + 0.1f;	// position
		float colstart = boxtop + buffer;				// position
		float colheight = 2*(boxHeight+buffer) - 0.1f;	// lenght
		float rotcol = 1.0f;
		float greencol = 0.0f;
	
		if (distanceGrasp > rgrasp) {collisionWarning.SetActive(false);}
		else if (cur_pos.y < boxbottom)
		{
			collisionWarning.SetActive(CollisionWarning);
			scalecol.y = colheight;
			rotcol = 1.0f;
			greencol = 0.0f;
		}
		else if (cur_pos.y > colstart)
		{
			collisionWarning.SetActive(CollisionWarning);
			scalecol.y = 0.0005f;
			rotcol = 0.0f;
			greencol = 1.0f;
		}
		else
		{
			collisionWarning.SetActive(CollisionWarning);
			scalecol.y = (colheight-0.0005f)/(boxbottom-colstart) * cur_pos.y + colheight - (colheight-0.0005f)*boxbottom/(boxbottom-colstart);
			greencol = 1/(boxtop-boxbottom) * cur_pos.y - 1*boxbottom/(boxtop-boxbottom);
		}

		collisionWarning.GetComponent<MeshRenderer>().material.color = new Color(rotcol, greencol, 0.0f, 1.0f);
		collisionWarning.transform.localScale = scalecol;

	}

	// ******************************
	// ***** BoxCollisionWarning ****
	// ******************************


	// ******************************
	// **** WorkspaceRestriction ****
	// ******************************

	// RestrictionFront
	float xFront;
	float distanceRestrictionFront = positionRestrictionFront - Convert.ToSingle(cur_pos.z);

	if (distanceRestrictionFront > distanceRestricitionFeedbackStart) {xFront = 0;}
	else if (distanceRestrictionFront < 0) {xFront = distanceRestricitionFeedbackStart;}
	else {xFront = distanceRestricitionFeedbackStart - distanceRestrictionFront;}

	float alphaFront = xFront*xFront/(distanceRestricitionFeedbackStart*distanceRestricitionFeedbackStart);
	restrictionFront.GetComponent<MeshRenderer>().material.color = new Color(255f, 0.0f, 0.0f, alphaFront);
	restrictionFront.SetActive(WorkspaceRestrictions);

	// RestrictionBack
	float xBack;
	float distanceRestrictionBack = Convert.ToSingle(cur_pos.z) - positionRestrictionBack;

	if (distanceRestrictionBack > distanceRestricitionFeedbackStart) {xBack = 0;}
	else if (distanceRestrictionBack < 0) {xBack = distanceRestricitionFeedbackStart;}
	else {xBack = distanceRestricitionFeedbackStart - distanceRestrictionBack;}

	float alphaBack = xBack*xBack/(distanceRestricitionFeedbackStart*distanceRestricitionFeedbackStart);
	restrictionBack.GetComponent<MeshRenderer>().material.color = new Color(255f, 0.0f, 0.0f, alphaBack);
	restrictionBack.SetActive(WorkspaceRestrictions);

	// RestrictionRight
	float xRight;
	float distanceRestrictionRight = positionRestrictionRight - Convert.ToSingle(cur_pos.x);

	if (distanceRestrictionRight > distanceRestricitionFeedbackStart) {xRight = 0;}
	else if (distanceRestrictionRight < 0) {xRight = distanceRestricitionFeedbackStart;}
	else {xRight = distanceRestricitionFeedbackStart - distanceRestrictionRight;}

	float alphaRight = xRight*xRight/(distanceRestricitionFeedbackStart*distanceRestricitionFeedbackStart);
	restrictionRight.GetComponent<MeshRenderer>().material.color = new Color(255f, 0.0f, 0.0f, alphaRight);
	restrictionRight.SetActive(WorkspaceRestrictions);

	// RestrictionLeft
	float xLeft;
	float distanceRestrictionLeft = Convert.ToSingle(cur_pos.x) - positionRestrictionLeft;

	if (distanceRestrictionLeft > distanceRestricitionFeedbackStart) {xLeft = 0;}
	else if (distanceRestrictionLeft < 0) {xLeft = distanceRestricitionFeedbackStart;}
	else {xLeft = distanceRestricitionFeedbackStart - distanceRestrictionLeft;}

	float alphaLeft = xLeft*xLeft/(distanceRestricitionFeedbackStart*distanceRestricitionFeedbackStart);
	restrictionLeft.GetComponent<MeshRenderer>().material.color = new Color(255f, 0.0f, 0.0f, alphaLeft);
	restrictionLeft.SetActive(WorkspaceRestrictions);
	
	// Force Restrictions
	if (cur_pos.z > positionRestrictionFront) {cur_pos.z = positionRestrictionFront;}
	if (cur_pos.z < positionRestrictionBack) {cur_pos.z = positionRestrictionBack;}
	if (cur_pos.x > positionRestrictionRight) {cur_pos.x = positionRestrictionRight;}
	if (cur_pos.x < positionRestrictionLeft) {cur_pos.x = positionRestrictionLeft;}
	uiOperatorPosition.transform.position = cur_pos;

	// ******************************
	// **** WorkspaceRestriction ****
	// ******************************


	// ******************************
	// ****** Z-LevelAssistance *****
	// ******************************

	// Get parameters
	if (isItem) {itemSize = itemSize;} else {itemSize = 0.0f;}
	float rLow = rgrasp;				// Adjust for real robot
	float rHigh = diagGrasp + buffer;

	float restrictionZLow = groundLevel + operatorHeight + itemSize;
	float restrictionZHigh = restrictionZLow + boxHeight;

	// Somewhere
	if (distanceGrasp > rLow)
	{
		if (cur_pos.y < restrictionZLow)
		{
			cur_pos.y = restrictionZLow;
		}
	}
	// Within RestrictionRadius
	else if (Convert.ToSingle(cur_pos.x) > 0.01 || Convert.ToSingle(cur_pos.x) < -0.2 || Convert.ToSingle(cur_pos.z) < 0.45 || Convert.ToSingle(cur_pos.z) > 0.84)
	{
		if (distanceGrasp < rHigh)
		{
			if (cur_pos.y < restrictionZHigh)
			{
			cur_pos.y = restrictionZHigh;
			}
		}
		else if (cur_pos.y < (restrictionZHigh-restrictionZLow)/(rHigh-rLow) * distanceGrasp + restrictionZLow - (restrictionZHigh-restrictionZLow)*rLow/(rHigh-rLow))
		{
			cur_pos.y = (restrictionZHigh-restrictionZLow)/(rHigh-rLow) * distanceGrasp + restrictionZLow - (restrictionZHigh-restrictionZLow)*rLow/(rHigh-rLow);
		}
	}
	// Within grasping area
	else
	{
		if (cur_pos.y < restrictionZLow)
		{
		cur_pos.y = restrictionZLow;
		}
	}

	if (ZLevelAssistance) {uiOperatorPosition.transform.position = cur_pos;}

	// ******************************
	// ****** Z-LevelAssistance *****
	// ******************************
  
	}
}


