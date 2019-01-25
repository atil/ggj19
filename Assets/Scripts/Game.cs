using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Game : MonoBehaviour
{
    public AnimationCurve SuccEffect;

    [Space]

    public GameObject BitPrefab;

    [Space]

    public BoxCollider2D HomeA;
    public Transform SpawnPointA;
    public BoxCollider2D PressZoneA;
    public Transform ReserveRootA;

    [Space]

    public BoxCollider2D HomeB;
    public Transform SpawnPointB;
    public BoxCollider2D PressZoneB;
    public Transform ReserveRootB;

    private bool _goingRight = true;
    private List<GameObject> _bits = new List<GameObject>();

    private int _reserveBitCountA = 5;
    private int _reserveBitCountB = 0;
    private float _bitSpeed = 2.5f;

    private void Start()
    {

        UpdateReserveRoot(ReserveRootA, _reserveBitCountA);
        UpdateReserveRoot(ReserveRootB, _reserveBitCountB);
    }

    private void Update()
    {
        if (_goingRight)
        {
            Move(KeyCode.A, KeyCode.L, Vector3.right, HomeB, SpawnPointA, PressZoneB,
                ReserveRootA, ReserveRootB,
                ref _reserveBitCountA, ref _reserveBitCountB);
        }
        else
        {
            Move(KeyCode.L, KeyCode.A, Vector3.left, HomeA, SpawnPointB, PressZoneA, 
                ReserveRootB, ReserveRootA,
                ref _reserveBitCountB, ref _reserveBitCountA);
        }
    }

    private void Move(KeyCode sendKey, KeyCode recvKey, Vector3 dir, BoxCollider2D targetHome, Transform spawnPoint, BoxCollider2D pressZone,
        Transform senderReserveRoot, Transform recvReserveRoot,
        ref int sendReserv, ref int recvReserv)
    {
        if (Input.GetKeyDown(sendKey) && sendReserv > 0)
        {
            sendReserv--;
            UpdateReserveRoot(senderReserveRoot, sendReserv);
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
                    UpdateReserveRoot(recvReserveRoot, recvReserv);
                    StartCoroutine(RunSuccEffectAt(bit.transform.position));
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
                recvReserv = 0;
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

    private void UpdateReserveRoot(Transform root, int reservCount)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Destroy(root.GetChild(i).gameObject);
        }

        for (int i = 0; i < reservCount; i++)
        {
            StartCoroutine(RunSpawnEffectAt(root.transform.position + Vector3.down * i * 0.5f, root));
        }
    }

    private IEnumerator RunSpawnEffectAt(Vector3 initPos, Transform root)
    {
        const float duration = 0.25f;
        Vector3 startScale = BitPrefab.transform.localScale * 2;
        Vector3 targetScale = BitPrefab.transform.localScale;
        GameObject go = Instantiate(BitPrefab, initPos, Quaternion.identity);

        SpriteRenderer r = go.GetComponent<SpriteRenderer>();
        Color startColor = new Color(r.color.r, r.color.g, r.color.b, 0);
        Color targetColor = r.color;

        for (float f = 0; f < duration; f += Time.deltaTime)
        {
            float t = SuccEffect.Evaluate(f / duration);
            go.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            r.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        go.transform.SetParent(root);
    }

    private IEnumerator RunSuccEffectAt(Vector3 initPos)
    {
        const float duration = 0.25f;
        Vector3 startScale = BitPrefab.transform.localScale;
        Vector3 targetScale = BitPrefab.transform.localScale * 2;
        GameObject go = Instantiate(BitPrefab, initPos, Quaternion.identity);

        SpriteRenderer r = go.GetComponent<SpriteRenderer>();
        Color startColor = r.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0);

        for (float f = 0; f < duration; f += Time.deltaTime)
        {
            float t = SuccEffect.Evaluate(f / duration);
            go.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            r.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        Destroy(go);
    }
}
