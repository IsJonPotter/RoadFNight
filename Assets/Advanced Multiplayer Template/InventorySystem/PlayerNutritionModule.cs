﻿using UnityEngine;
using Mirror;

public class PlayerNutritionModule : NetworkBehaviour {

	[SyncVar] public int value;

	private void OnValidate() {
		syncMode = SyncMode.Owner;
		syncInterval = 0f;
	}

	[SerializeField] private float _intervalInSeconds;
	private float _timeLeft;

	private void Update() {
		if (isServer) {
			if (value <= 0) {
				return;
			}

			_timeLeft -= Time.deltaTime;

			if (_timeLeft <= 0) {
				_timeLeft = _intervalInSeconds;
				value--;
			}
		}
	}
}
