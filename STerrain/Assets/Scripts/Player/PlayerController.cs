using UnityEngine;

namespace STerrain.Player
{
    /// <summary>
    /// Handles player movement and jumping.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float _playerSpeed = 2.0f;
        [SerializeField] private float _jumpHeight = 1.0f;
        [SerializeField] private float _gravityValue = -9.81f;
        [SerializeField] private bool _allowFlight = false;

        private CharacterController _characterController;
        private Vector3 _playerVelocity;
        private bool _playerIsGrounded;
        private Transform _cameraTransform;

        private void Start()
        {
            _characterController = GetComponent<CharacterController>();
            _cameraTransform = Camera.main.transform;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            _playerIsGrounded = _characterController.isGrounded;

            //Check if character grounded and falling at the same time.
            if (_playerIsGrounded && _playerVelocity.y < 0)
            {
                //Set y velocity to 0 so that the character does not fall through the ground.
                _playerVelocity.y = 0;
            }

            //Get movement input.\
            var move = new Vector3(
                Input.GetAxis("Horizontal"),
                0f,
                Input.GetAxis("Vertical"));
            move = _cameraTransform.forward * move.z + _cameraTransform.right * move.x;
            move.y = 0;

            _characterController.Move(move * Time.deltaTime * _playerSpeed);

            //Changes the height position of the character when jump is pressed.
            if (Input.GetKey(KeyCode.Space) && _playerIsGrounded)
            {
                _playerVelocity.y += Mathf.Sqrt(_jumpHeight * -3.0f * _gravityValue);
            }

            if (_allowFlight)
            {
                if (Input.GetKey(KeyCode.Space))
                {
                    _playerVelocity.y += Time.deltaTime * _playerSpeed;
                }
                else if (Input.GetKey(KeyCode.LeftShift))
                {
                    _playerVelocity.y -= Time.deltaTime * _playerSpeed;
                }
                else
                {
                    _playerVelocity.y = 0;
                }
            }
            else
            {
                //Apply gravity.
                _playerVelocity.y += _gravityValue * Time.deltaTime;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }

            _characterController.Move(_playerVelocity * Time.deltaTime);
        }
    }
}