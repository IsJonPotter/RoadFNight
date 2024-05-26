﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using RedicionStudio.UIUtils;

namespace RedicionStudio.InventorySystem {

	public class SyncDictionaryIntDouble : SyncDictionary<int, double> { }

	public class PlayerInventoryModule : Inventory {

		[Header("Player Modules")]
		public Player player;
		public PlayerNutritionModule playerNutrition;

		[Space]
		public AudioSource audioSource;

        [Space]
        public ManageTPController TPControllerManager;

        [Space]
        public GameObject bulletPrefab;
        public GameObject rocketPrefab;
        public float bulletSpeed;
        Transform _bulletSpawnPointPosition;

        [Space]
        public GameObject cartridgeEjectPrefab;
        Transform _cartridgeEjectSpawnPointPosition;

        [Space]
        public bool inPropertyArea = false;

        [Space]
        public bool inShop = false;

        [Space]
        public bool inCar = false;

        [Space]
        public bool usesParachute = false;

        [Space]
        public bool isAiming = false;

        private bool isWeaponWheelActive = false;
        RectTransform rectTransform;

        private StarterAssets.StarterAssetsInputs _input;

        public ChatSystem chatWindow;

        public void LoadInventory() {
			for (int i = 0; i < 67; i++) {
				slots.Add(new ItemSlot());
			}

#if UNITY_SERVER || UNITY_EDITOR // ?
			MasterServer.MSClient.GetInventory(player.id, (inventoryData) => {
				if (inventoryData == null || inventoryData.Length < 1) { // ?
					return;
				}
				ItemSlot slot;
				for (int i = 0; i < inventoryData.Length; i++) {
					slot = new ItemSlot();
					Debug.Log(inventoryData[i].hash);
					slot.item.hash = inventoryData[i].hash;
					slot.amount = inventoryData[i].amount;
					slot.item.currentShelfLifeInSeconds = inventoryData[i].shelfLife;
					slots[i] = slot;
				}
			});
#endif
		}

		[Command]
		private void CmdInventoryMerge(int from, int to) {
			if (0 <= from && from < slots.Count &&
				0 <= to && to < slots.Count &&
				from != to) {
				ItemSlot fromSlot = slots[from];
				ItemSlot toSlot = slots[to];
				if (fromSlot.amount > 0 && toSlot.amount > 0) {
					if (fromSlot.item.Equals(toSlot.item)) {
						int put = toSlot.IncreaseBy(fromSlot.amount);
						fromSlot.DecreaseBy(put);
						slots[from] = fromSlot;
						slots[to] = toSlot;
					}
				}
			}
		}

		[Command]
		private void CmdInventorySplit(int from, int to) {
			if (0 <= from && from < slots.Count &&
				0 <= to && to < slots.Count &&
				from != to) {
				ItemSlot fromSlot = slots[from];
				ItemSlot toSlot = slots[to];
				if (fromSlot.amount >= 2 && toSlot.amount == 0) {
					toSlot = fromSlot;

					toSlot.amount = fromSlot.amount / 2;
					fromSlot.amount -= toSlot.amount;

					slots[from] = fromSlot;
					slots[to] = toSlot;
				}
			}
		}

		[Command]
		private void CmdSwapInventoryInventory(int from, int to) {
			if (0 <= from && from < slots.Count &&
				0 <= to && to < slots.Count &&
				from != to) {
				ItemSlot fromSlot = slots[from];
				if ((to == 0 && !(fromSlot.item.itemSO is WeaponItemSO)) ||
					(to == 1 && !(fromSlot.item.itemSO is AmmoItemSO)) ||
                    (to == 2 && !(fromSlot.item.itemSO is OutfitItemSO)) ||
                    (to == 3 && !(fromSlot.item.itemSO is CompanionItemSO))){
                    return;
				}
				slots[from] = slots[to];
				slots[to] = fromSlot;
			}
		}

		private void Start() {
			if (isLocalPlayer) {
				UIPlayerInventory.playerInventory = this;
				slots.Callback += Slots_Callback;
				UIPlayerInventory.InstanceRefresh();

				UIDragAndDrop.OnDragAndClearAction = CmdDropItem;
				UIDragAndDrop.OnDragAndDropAction = (from, to) => {
					if (slots[from].amount > 0 && slots[to].amount > 0 &&
					slots[from].item.Equals(slots[to].item)) {
						CmdInventoryMerge(from, to);
					}
					else if (_keyboard != null && _keyboard.shiftKey.isPressed) {
						CmdInventorySplit(from, to);
					}
					else {
						CmdSwapInventoryInventory(from, to);
					}
				};
			}
            if (chatWindow == null)
                chatWindow = GameObject.FindGameObjectWithTag("ChatWindow").GetComponent<ChatSystem>();
        }

		private void Slots_Callback(SyncList<ItemSlot>.Operation op, int itemIndex, ItemSlot oldItem, ItemSlot newItem) {
			UIPlayerInventory.InstanceRefresh();
		}

		/// <summary>
		/// (Server)
		/// </summary>
		private void DropItem(Item item, int amount, bool remove) {
			Vector2 randomPoint = Random.insideUnitCircle * 2f;
			Vector3 position = new Vector3(transform.position.x + randomPoint.x, transform.position.y, transform.position.z + randomPoint.y);

			GameObject gO = Instantiate(ConfigurationSO.Instance.itemDropPrefab, position, Quaternion.identity);
			ItemDrop itemDrop = gO.GetComponent<ItemDrop>();
            itemDrop.remove = remove;
            itemDrop.item = item;
			itemDrop.amount = amount;
			NetworkServer.Spawn(gO);
		}

		/// <summary>
		/// (Server)
		/// </summary>
		private void DropItemAndClearSlot(int slotIndex, bool remove) {
			ItemSlot slot = slots[slotIndex];
			DropItem(slot.item, slot.amount, remove);
			slot.amount = 0;
			slots[slotIndex] = slot;
		}

		[Command]
		public void CmdDropItem(int index) {
            if(index > 3) // Ensures that no item can be dropped as long as it is equipped.
            {
                if (0 <= index && index < slots.Count && slots[index].amount > 0){
                    DropItemAndClearSlot(index, false);
                }
            }
		}

        [Command]
        public void CmdDropAndRemoveItem(int index, bool remove)
        {
            if (index > 3) // Ensures that no item can be dropped as long as it is equipped.
            {
                if (0 <= index && index < slots.Count && slots[index].amount > 0)
                {
                    DropItemAndClearSlot(index, remove);
                }
            }
        }

        #region Cooldowns

        private Dictionary<int, double> _local_itemCooldowns = new Dictionary<int, double>();
		private readonly SyncDictionaryIntDouble _itemCooldowns = new SyncDictionaryIntDouble();

		public void SetCooldown(int itemSOHash, float cooldownInSeconds) {
			double cooldownEndTime = NetworkTime.time + cooldownInSeconds;

			if (isClient && !isServer) {
				_local_itemCooldowns[itemSOHash] = cooldownEndTime;
			}
			else {
				_itemCooldowns[itemSOHash] = cooldownEndTime;
			}
		}

		public float GetCooldown(int itemSOHash) {
			double cooldownEndTime;

			if (isClient && !isServer) {
				if (_local_itemCooldowns.TryGetValue(itemSOHash, out cooldownEndTime)) {
					return NetworkTime.time >= cooldownEndTime ? 0f : (float)(cooldownEndTime - NetworkTime.time);
				}
			}

			if (_itemCooldowns.TryGetValue(itemSOHash, out cooldownEndTime)) {
				return NetworkTime.time >= cooldownEndTime ? 0f : (float)(cooldownEndTime - NetworkTime.time);
			}

			return 0f;
		}

		#endregion

		[ClientRpc]
		public void RpcOnItemUsed(Item item) {
			if (item.itemSO is UseableItemSO usableItemSO) {
				usableItemSO.OnUsed(this);
			}
		}

		[Command]
		public void CmdUseItem(int slotIndex) {
			if (0 <= slotIndex && slotIndex < slots.Count && slots[slotIndex].amount > 0 && slots[slotIndex].item.itemSO is UseableItemSO usableItemSO && usableItemSO.CanBeUsed(this, slotIndex)) {
				usableItemSO.Use(this, slotIndex);
			}
		}

        [Command]
        public void CmdAim()
        {
            TPControllerManager.aimValue = 1;
        }

        public void ShootBullet()
        {
            if (!base.hasAuthority) return;

            _bulletSpawnPointPosition = this.GetComponent<ManageTPController>().CurrentWeaponBulletSpawnPoint;
            _cartridgeEjectSpawnPointPosition = this.GetComponent<ManageTPController>().CurrentCartridgeEjectSpawnPoint;
            string currentBulletName = this.GetComponent<ManageTPController>().CurrentWeaponManager.WeaponBulletPrefab.name;
            //bulletSpeed = this.GetComponent<ManageTPController>().CurrentWeaponManager.BulletSpeed;
            this.GetComponent<ManageTPController>().Shoot();
            //CmdSetAttackerUsername(username);

            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));

            int layerToIgnore = 4; // Replace with the layer you want to ignore
            LayerMask layerMask = ~(1 << layerToIgnore);
            RaycastHit hit;
            Vector3 collisionPoint;
            if (Physics.Raycast(ray, out hit, 50f, layerMask))
            {
                collisionPoint = hit.point;
            }
            else
            {
                collisionPoint = ray.GetPoint(50f);
            }

            Vector3 bulletVector = (collisionPoint - _bulletSpawnPointPosition.transform.position).normalized;




            if (currentBulletName == "Bullet")
                CmdShootBullet(_bulletSpawnPointPosition.position, _bulletSpawnPointPosition.rotation, _cartridgeEjectSpawnPointPosition.position, _cartridgeEjectSpawnPointPosition.rotation, bulletVector, bulletSpeed);
            else
                CmdShootRocket(_bulletSpawnPointPosition.position, _bulletSpawnPointPosition.rotation, bulletVector, bulletSpeed);
        }

        [Command]
        void CmdShootBullet(Vector3 _position, Quaternion _rotation, Vector3 _cartridgeEjectPosition, Quaternion _cartridgeEjectRotation, Vector3 _bulletVector, float _bulletSpeed)
        {
            GameObject Bullet = Instantiate(bulletPrefab, _position, _rotation) as GameObject;

            Bullet.GetComponent<Rigidbody>().velocity = _bulletVector * _bulletSpeed;

            //Bullet.GetComponent<NetworkBullet>().SetupProjectile(this.GetComponent<Player>().username, hasAuthority);

            NetworkServer.Spawn(Bullet);

            NetworkBullet bullet = Bullet.GetComponent<NetworkBullet>();
            bullet.netIdentity.AssignClientAuthority(this.connectionToClient);

            bullet.SetupProjectile_ServerSide();

            RpcBulletFired(bullet, _bulletVector, _bulletSpeed);


            GameObject _cartridgeEject = Instantiate(cartridgeEjectPrefab, _cartridgeEjectPosition, _cartridgeEjectRotation) as GameObject;



            NetworkServer.Spawn(_cartridgeEject, connectionToClient);
        }

        [ClientRpc]
        void RpcBulletFired(NetworkBullet Bullet, Vector3 _bulletVector, float _bulletSpeed)
        {
            Bullet.GetComponent<NetworkBullet>().SetupProjectile(currentPlayerUsername(), hasAuthority);

            //Bullet.GetComponent<Rigidbody>().AddForce(_bulletVector * _bulletSpeed);
        }

        [Command]
        void CmdShootRocket(Vector3 _position, Quaternion _rotation, Vector3 _bulletVector, float _bulletSpeed)
        {
            GameObject Bullet = Instantiate(rocketPrefab, _position, _rotation) as GameObject;

            //Bullet.GetComponent<Rigidbody>().AddForce(_bulletVector * _bulletSpeed);

            //Bullet.GetComponent<NetworkBullet>().SetupProjectile(this.GetComponent<Player>().username, hasAuthority);

            NetworkServer.Spawn(Bullet);

            NetworkRocket bullet = Bullet.GetComponent<NetworkRocket>();
            bullet.netIdentity.AssignClientAuthority(this.connectionToClient);

            bullet.SetupProjectile_ServerSide();

            RpcRocketFired(bullet, _bulletVector, _bulletSpeed);
        }

        [ClientRpc]
        void RpcRocketFired(NetworkRocket Bullet, Vector3 _bulletVector, float _bulletSpeed)
        {
            Bullet.GetComponent<NetworkRocket>().SetupProjectile(currentPlayerUsername(), hasAuthority);

            Bullet.GetComponent<Rigidbody>().AddForce(_bulletVector * _bulletSpeed);
        }

        public string currentPlayerUsername()
        {
            return GetComponent<Player>().username;
        }

        /*[Command]
        void CmdSetAttackerUsername(string _username)
        {
            TPControllerManager.GetComponent<Health>().attackerUsername = _username;
        }*/

        private static Keyboard _keyboard;
		private static Mouse _mouse;

		public static bool inMenu;
        public static bool inWeaponWheel;

        private void OnDestroy() {
			if (isLocalPlayer) {
				UIPlayerInventory.playerInventory = null;
				slots.Callback -= Slots_Callback;
			}
		}

		private int _index;
		private double _interval = 60f;
		private double _lastTime;
		private ItemSlot _slot;
		private void Update() {
            _input = GameObject.FindGameObjectWithTag("InputManager").GetComponent<StarterAssets.StarterAssetsInputs>();
            if (isServer) {
				if (NetworkTime.time >= _lastTime + _interval) {
					for (_index = 0; _index < slots.Count; _index++) {
						_slot = slots[_index];
						if (_slot.amount > 0 && _slot.item.itemSO != null && _slot.item.itemSO is ConsumableItemSO) {
							if (_slot.item.currentShelfLifeInSeconds > 0f) {
								_slot.item.currentShelfLifeInSeconds -= (float)_interval;
							}
							else {
								_slot.item = new Item();
								_slot.amount = 0;
							}
							slots[_index] = _slot;
						}
					}
					_lastTime = NetworkTime.time;
				}
			}

			if (!isLocalPlayer) {
				return;
			}

			_keyboard = Keyboard.current;
			_mouse = Mouse.current;

			if (_keyboard == null || _mouse == null) {
				return;
			}

			if (_keyboard.tabKey.wasPressedThisFrame && !inShop) {
				inMenu = !inMenu;
				if (inMenu) {
					if (BSystem.BSystem.inMenu) {
						BSystem.BSystem.inMenu = false;
						BSystemUI.Instance.SetActive(false);

					}
					UIPlayerInventory.SetActive(true);
                    UIPlayerInventory.InventoryUI.SetActive(true);
                    TPController.TPCameraController.LockCursor(false);
				}
				else {
					UIPlayerInventory.SetActive(false);
                    UIPlayerInventory.InventoryUI.SetActive(false);
                    TPController.TPCameraController.LockCursor(true);
				}
			}

            if (_input.weaponWheel & !inMenu)
            {
                if (!isWeaponWheelActive)
                {
                    inWeaponWheel = !inWeaponWheel;
                    if (inWeaponWheel)
                    {
                        isWeaponWheelActive = true;
                        if (BSystem.BSystem.inMenu)
                        {
                            BSystem.BSystem.inMenu = false;
                            BSystemUI.Instance.SetActive(false);

                        }
                        UIPlayerInventory.WeaponWheel.SetActive(true);
                        TPController.TPCameraController.LockCursor(false);
                        rectTransform = UIPlayerInventory.WeaponWheel.GetComponent<WeaponWheelSystem>().MousePositionText.GetComponent<RectTransform>();
                        rectTransform.anchorMin = new Vector2(0, 0);
                        rectTransform.anchorMax = new Vector2(0, 0);
                        foreach (ItemSlot slot in slots)
                        {
                            if(slot.item.itemSO != null)
                            {
                                bool alreadyRegistered = new bool();
                                foreach (WeaponWheelItem item in UIPlayerInventory.WeaponWheel.GetComponent<WeaponWheelSystem>().weapons)
                                {
                                    if (item.WeaponName == slot.item.itemSO.uniqueName)
                                        alreadyRegistered = true;
                                }

                                if (!alreadyRegistered)
                                {
                                    WeaponWheelItem weaponItem = new WeaponWheelItem();
                                    weaponItem.WeaponName = slot.item.itemSO.uniqueName;
                                    weaponItem.InfoText = slot.item.itemSO.tooltipText;
                                    weaponItem.type = slot.item.itemSO.weaponType;
                                    UIPlayerInventory.WeaponWheel.GetComponent<WeaponWheelSystem>().weapons.Add(weaponItem);
                                    if (weaponItem.type == ItemSO.WeaponType.Item)
                                        UIPlayerInventory.WeaponWheel.GetComponent<WeaponWheelSystem>().weapons.Remove(weaponItem);
                                }
                            }
                        }
                    }
                    else
                    {
                        UIPlayerInventory.WeaponWheel.SetActive(false);
                        TPController.TPCameraController.LockCursor(true);
                        isWeaponWheelActive = false;
                    }
                }
                if(isWeaponWheelActive)
                {
                    rectTransform.anchoredPosition3D = Input.mousePosition;
                    UIPlayerInventory.WeaponWheel.GetComponent<WeaponWheelSystem>().MousePositionText.text = Input.mousePosition.ToString();
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
            if (!_input.weaponWheel)
            {
                UIPlayerInventory.WeaponWheel.SetActive(false);
                if(!BSystem.BSystem.inMenu && !inMenu && !GetComponent<EmoteWheel>().inEmoteWheel && !inShop && !chatWindow.GetComponent<ChatSystem>().isChatOpen)
                {
                    TPController.TPCameraController.LockCursor(true);
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                isWeaponWheelActive = false;
                inWeaponWheel = false;
            }

            _slot = slots[0];
			if (!BSystem.BSystem.inMenu && !inMenu && !inWeaponWheel && !GetComponent<EmoteWheel>().inEmoteWheel && !inPropertyArea && !inShop && !inCar && !usesParachute && !this.GetComponent<EmoteWheel>().isPlayingAnimation && isAiming && !this.GetComponent<Health>().isDeath && _input.shoot && _slot.amount > 0 && _slot.item.itemSO != null && _slot.item.itemSO is WeaponItemSO weaponItemSO) {
				_interval = weaponItemSO.cooldownInSeconds;
				if (NetworkTime.time >= _lastTime + _interval) {
					if (weaponItemSO.automatic) {
						CmdUseItem(0);
					}
					else if (_mouse.leftButton.wasPressedThisFrame || Gamepad.current.rightTrigger.wasPressedThisFrame) {
						CmdUseItem(0);
                    }
					_lastTime = NetworkTime.time;
				}
			}
            //Aim
            if (!BSystem.BSystem.inMenu & !inMenu & !inPropertyArea & !inShop & !inCar & !usesParachute & !this.GetComponent<Health>().isDeath & !this.GetComponent<EmoteWheel>().isPlayingAnimation & _input.aim & _slot.amount > 0 & _slot.item.itemSO != null & _slot.item.itemSO is WeaponItemSO)
            {
                CmdAim();
            }
        }

		[Space]
		[SerializeField] private Transform _gFX;

		private void LateUpdate() {
            if(chatWindow == null)
                chatWindow = chatWindow = GameObject.FindGameObjectWithTag("ChatWindow").GetComponent<ChatSystem>();
            if (isServer) {
				return;
			}

			for (int i = 0; i < _gFX.childCount; i++) {
				_gFX.GetChild(i).gameObject.SetActive(false);
                _gFX.GetChild(i).GetComponent<WeaponManager>().enabled = false;
            }
			if (!this.GetComponent<Health>().isDeath && !inCar && !usesParachute && !this.GetComponent<EmoteWheel>().isPlayingAnimation && slots[0].amount > 0 && slots[0].item.itemSO != null) {
                this.GetComponent<Animator>().SetLayerWeight(1, 1);
                for (int i = 0; i < _gFX.childCount; i++) {
					if (_gFX.GetChild(i).name == slots[0].item.itemSO.uniqueName) {
						_gFX.GetChild(i).gameObject.SetActive(true);
                        _gFX.GetChild(i).GetComponent<WeaponManager>().enabled = true;
                        this.GetComponent<ManageTPController>().CurrentWeaponManager = _gFX.GetChild(i).GetComponent<WeaponManager>();
                        this.GetComponent<ManageTPController>().CurrentWeaponBulletSpawnPoint = _gFX.GetChild(i).GetComponent<WeaponManager>().CurrentWeaponBulletSpawnPoint;
                        this.GetComponent<ManageTPController>().CurrentCartridgeEjectSpawnPoint = _gFX.GetChild(i).GetComponent<WeaponManager>().CartridgeEjectEffectSpawnPoint;
                    }
				}
			}
            else
            {
                this.GetComponent<Animator>().SetLayerWeight(1, 0);
                this.GetComponent<ManageTPController>().PlayerRig.weight = 0;
            }
		}
	}
}
