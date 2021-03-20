using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class SubPartInfo {
	[SerializeField]
	private string subPartType;
	[SerializeField]
	private List<GameObject> requiredOtherSubPartObjects;
	[SerializeField]
	private List<GameObject> requiredOneOfOtherSubPartObjects;
	[SerializeField]
	private List<GameObject> prohibitingOtherSubPartObjects;

	public SubPartInfo(string subPartType, List<GameObject> requiredOtherSubPartObjects,
		List<GameObject> requiredOneOfOtherSubPartObjects,
		List<GameObject> prohibitingOtherSubPartObjects) {

		this.subPartType = subPartType;

		this.requiredOtherSubPartObjects = requiredOtherSubPartObjects;
		if (this.requiredOtherSubPartObjects == null) {
			this.requiredOtherSubPartObjects = new List<GameObject>();
		}

		this.requiredOneOfOtherSubPartObjects = requiredOneOfOtherSubPartObjects;
		if (this.requiredOneOfOtherSubPartObjects == null) {
			this.requiredOneOfOtherSubPartObjects = new List<GameObject>();
		}

		this.prohibitingOtherSubPartObjects = prohibitingOtherSubPartObjects;
		if (this.prohibitingOtherSubPartObjects == null) {
			this.prohibitingOtherSubPartObjects = new List<GameObject>();
		}
	}

	public string GetSubPartType() {
		return subPartType;
	}

	public List<GameObject> GetRequiredOtherSubPartObjects() {
		return requiredOtherSubPartObjects;
	}

	public List<GameObject> GetRequiredOneOfOtherSubPartObjects() {
		return requiredOneOfOtherSubPartObjects;
	}

	public List<GameObject> GetProhibitingOtherSubPartObjects() {
		return prohibitingOtherSubPartObjects;
	}
}
