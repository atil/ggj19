using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Game : MonoBehaviour
{
    public GameObject BitPrefab;

    public BoxCollider2D HomeA;
    public Transform SpawnPointA;
    public BoxCollider2D PressZoneA;
    public TMPro.TextMeshPro ReserveTextA;

    [Space]

    public BoxCollider2D HomeB;
    public Transform SpawnPointB;
    public BoxCollider2D PressZoneB;
    public TMPro.TextMeshPro ReserveTextB;

    private bool _goingRight = true;
    private List<GameObject> _bits = new List<GameObject>();

    private int _reserveBitCountA = 5;
    private int _reserveBitCountB = 0;
    private float _bitSpeed = 2.5f;

    private void Start()
    {
        ReserveTextA.text = _reserveBitCountA.ToString();
        ReserveTextB.text = _reserveBitCountB.ToString();
    }

    private void Update()
    {
        if (_goingRight)
        {
            Move(KeyCode.A, KeyCode.L, Vector3.right, HomeB, SpawnPointA, PressZoneB, ReserveTextA, ReserveTextB, ref _reserveBitCountA, ref _reserveBitCountB);
        }
        else
        {
            Move(KeyCode.L, KeyCode.A, Vector3.left, HomeA, SpawnPointB, PressZoneA, ReserveTextB, ReserveTextA, ref _reserveBitCountB, ref _reserveBitCountA);
        }
    }

    private void Move(KeyCode sendKey, KeyCode recvKey, Vector3 dir, BoxCollider2D targetHome, Transform spawnPoint, BoxCollider2D pressZone,
        TMPro.TextMeshPro senderReservText, TMPro.TextMeshPro recvReservText,
        ref int sendReserv, ref int recvReserv)
    {
        // TODO: Add cooldown to this
        if (Input.GetKeyDown(sendKey) && sendReserv > 0)
        {
            sendReserv--;
            senderReservText.text = sendReserv.ToString();
            var bit = Instantiate(BitPrefab, spawnPoint.position, Quaternion.identity);
            _bits.Add(bit);
        }

        foreach (var bit in _bits)
        {
            bit.transform.position += dir * _bitSpeed * Time.deltaTime;
        }

        List<GameObject> removedBits = new List<GameObject>();
        if (Input.GetKeyDown(recvKey))
        {
            bool succ = false;
            foreach (var bit in _bits)
            {
                if (pressZone.OverlapPoint(bit.transform.position))
                {
                    // Succ
                    removedBits.Add(bit);
                    succ = true;

                    recvReserv++;
                    recvReservText.text = recvReserv.ToString();
                    
                }
            }
            if (!succ)
            {
                // Bosa basti
                // TODO: Decide:
                // - Speed up temporary?
                // - Reduce reserve count?

            }
        }

        foreach (var bit in _bits)
        {
            if (targetHome.OverlapPoint(bit.transform.position))
            {
                // Girdi
                removedBits.Add(bit);
            }
        }
        foreach (var bit in removedBits)
        {
            Destroy(bit);
        }
        _bits = _bits.Except(removedBits).ToList();

        if (sendReserv == 0 && _bits.Count == 0)
        {
            _goingRight = !_goingRight;
        }
    }
}
