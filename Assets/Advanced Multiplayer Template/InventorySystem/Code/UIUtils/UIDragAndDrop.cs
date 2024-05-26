﻿using UnityEngine;
using UnityEngine.EventSystems;

namespace RedicionStudio.UIUtils {

	public class UIDragAndDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler {

		private UnityEngine.UI.Button _uIButton;
		[SerializeField] private PointerEventData.InputButton _button;
		[SerializeField] private GameObject _prefab;

		private static GameObject _currentGO;

		[HideInInspector] public bool draggable = true, draggedToSlot;

		private void Awake() {
			_uIButton = GetComponent<UnityEngine.UI.Button>();
		}

		public void OnBeginDrag(PointerEventData eventData) {
			if (draggable && eventData.button == _button) {
				_currentGO = Instantiate(_prefab, transform.position, Quaternion.identity);
				_currentGO.transform.SetParent(transform.root, true); // ?
				_currentGO.transform.SetAsLastSibling();
				_uIButton.interactable = false;
			}
		}

		public void OnDrag(PointerEventData eventData) {
			if (draggable && eventData.button == _button) {
				_currentGO.transform.position = eventData.position;
			}
		}

		public static System.Action<int> OnDragAndClearAction;

		private static int ToInt(string s) {
			return int.TryParse(s, out int result) ? result : -1;
		}

		public void OnEndDrag(PointerEventData eventData) {
			Destroy(_currentGO);

			if (draggable && eventData.button == _button) {
				if (!draggedToSlot && eventData.pointerEnter == null) {
					OnDragAndClearAction.Invoke(ToInt(name));
				}

				draggedToSlot = false;

				_uIButton.interactable = true;
			}
		}

		public static System.Action<int, int> OnDragAndDropAction;

		public void OnDrop(PointerEventData eventData) {
			if (eventData.button == _button) {
				UIDragAndDrop uIDragAndDrop = eventData.pointerDrag.GetComponent<UIDragAndDrop>(); // ?
				if (uIDragAndDrop != null && uIDragAndDrop.draggable) {
					uIDragAndDrop.draggedToSlot = true;
					if (uIDragAndDrop != this) {
						OnDragAndDropAction.Invoke(ToInt(uIDragAndDrop.name), ToInt(name));
					}
				}
			}
		}

		private void OnDisable() {
			Destroy(_currentGO);
		}

		private void OnDestroy() { // ?
			Destroy(_currentGO);
		}
	}
}
