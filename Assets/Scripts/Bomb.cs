using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Bomb : MonoBehaviour
{
    ///Inspector Variables
    [SerializeField] protected Sprite I_NormalSprite;
    [SerializeField] protected Sprite I_FlashingSprite;
    [SerializeField] protected float I_FlashingSpeed = 0.4f;
    [SerializeField] protected float I_TimeDelayForTankNearExposion = 0.4f;
    [SerializeField] protected GameObject I_DistanceColliderObject;

    ///Private Variables
    private SpriteRenderer m_SpriteRenderer;
    private ParticleSystem m_ParticleSystem;
    private int m_NumberInArray = -1;
    private float m_TimeUntilDestory = 0.0f;
    private Vector3 m_Position = Vector3.zero;
    private float m_TimeOfCreation = 0.0f;
    private bool m_HasCheckedForExternalExplodeCollider = false;

    ///Unity Functions
    private void Awake()
    {
        m_SpriteRenderer = GetComponent<SpriteRenderer>();
        m_ParticleSystem = GetComponentInChildren<ParticleSystem>();
    }
    private void FixedUpdate()
    {
        //Check if time from hit to explode finished
        if (m_TimeUntilDestory != 0.0f)
        {
            DestoryBomb();
        }
        else if (m_TimeOfCreation + 8.0f <= Time.fixedTime)
        {
            //Destroy
            Explode();
        }
        else if (m_TimeOfCreation + 6.0f <= Time.fixedTime)
        {
            //Run Flashing
            float interval = (Time.fixedTime - m_TimeOfCreation - 6.0f) / 2.0f / I_FlashingSpeed;
            float redOrYellow = Mathf.Floor((interval - Mathf.Floor(interval)) * 2.0f);

            if (redOrYellow == 0)
                m_SpriteRenderer.sprite = I_FlashingSprite;
            else
                m_SpriteRenderer.sprite = I_NormalSprite;
        }

        //TurnColliderOn
        GameObject parentBody = GameObject.Find(GlobalVariables.PlayerTankBodyName);
        if (parentBody.transform.parent == transform.parent)
        {
            if (Vector3.Distance(parentBody.transform.position, transform.position) > 2.7f)
                I_DistanceColliderObject.GetComponent<CircleCollider2D>().enabled = true;
        }
        else
        {
            for (int i = 0; i < transform.parent.childCount; i++)
            {
                if (transform.parent.GetChild(i).name == "Body")
                {
                    if (Vector3.Distance(transform.parent.GetChild(i).transform.position, transform.position) >= 2.0f)
                        I_DistanceColliderObject.GetComponent<CircleCollider2D>().enabled = true;
                }
            }
        }

        //Keep position constant
        transform.position = m_Position;
    }

    /// Private Functions
    private void Explode()
    {
        if (m_ParticleSystem.IsAlive())
        {
            return;
        }
        m_ParticleSystem.Play();
        m_TimeUntilDestory = Time.fixedTime + 1.0f;
        m_SpriteRenderer.sprite = null;

    }
    private void DestoryBomb()
    {
        //Leave time for explosion
        if (Time.fixedTime >= m_TimeUntilDestory)
        {
            m_ParticleSystem.Stop();
            m_TimeUntilDestory = 0.0f;

            if (GetComponentInParent<Tank>())
            {
                GetComponentInParent<Tank>().ExplodeBomb(m_NumberInArray);
            }
            else
            {
                GetComponentInParent<GlobalVariables>().ExplodeBomb(m_NumberInArray);
            }
        }
        else if (!m_HasCheckedForExternalExplodeCollider && Time.fixedTime >= m_TimeUntilDestory - 0.5f)
        {
            GameObject[] objects = GameObject.FindGameObjectsWithTag(GlobalVariables.TagBomb);
            bool[] isOn = new bool[objects.Length];
            for (int i = 0; i< objects.Length; i++)
            {
                if (objects[i].name == "DistanceCollider")
                {
                    isOn[i] = objects[i].GetComponent<CircleCollider2D>().enabled;
                    objects[i].GetComponent<CircleCollider2D>().enabled = false;
                }
            }

            //Set non trigger collider to true
            Collider2D[] hits = new Collider2D[16];
            m_HasCheckedForExternalExplodeCollider = true;
            I_DistanceColliderObject.GetComponent<CircleCollider2D>().enabled = true;
            ContactFilter2D contactF = new ContactFilter2D();
            contactF.useTriggers = true;
            contactF.SetLayerMask(1 << GlobalVariables.LayerTanks | 1 << GlobalVariables.LayerBullets | 1 << GlobalVariables.LayerWalls);
            int hit = I_DistanceColliderObject.GetComponent<CircleCollider2D>().OverlapCollider(contactF, hits);
            for (int i = 0; i < hit; i++)
            {
                if (hits[i])
                {
                    if (hits[i].gameObject.tag == GlobalVariables.TagBullet)
                    {
                        hits[i].GetComponent<Bullet>().DestroyBullet();
                    }
                    else if (hits[i].gameObject.layer == GlobalVariables.LayerTanks)
                    {
                        if (hits[i].transform.parent.GetComponent<Tank>().IsAlive())
                            hits[i].transform.parent.GetComponent<Tank>().DestroyTank();
                    }
                    else if (hits[i].gameObject.tag == GlobalVariables.TagBomb)
                    {
                        hits[i].transform.parent.GetComponent<Bomb>().Explode();
                    }
                    else if (hits[i].gameObject.layer == GlobalVariables.LayerWalls)
                    {
                        //do collision
                        Tilemap tileMap = hits[i].GetComponent<Tilemap>();
                        Grid tileMapGrid = tileMap.layoutGrid; 
                        Vector3Int pos = tileMapGrid.WorldToCell(m_Position);
                        for (int y = -3; y <= 3; y++)
                        {
                            for (int x = -3; x <= 3; x++)
                            {
                                Vector3Int tilePos = new Vector3Int(x + pos.x, y + pos.y, pos.z);
                                if (Vector3.Distance(tileMapGrid.LocalToCell(tilePos), transform.position) <= 2.5f && tileMap.GetTile(tilePos))
                                {
                                    if (tileMap.GetTile(tilePos).name == "DestructableRuleTile")
                                    {
                                        tileMap.SetTile(tilePos, null);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //Correct the colliders for bombs
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i].name == "DistanceCollider")
                {
                    objects[i].GetComponent<CircleCollider2D>().enabled = true;
                }
            }
        }
    }

    ///Public Functions
    public void Initialize(int numberInArray, Vector3 position, float rotation)
    {
        Vector3 rot = new Vector3(0.0f, 0.0f, rotation);
        transform.eulerAngles = rot;
        transform.position = position;
        m_SpriteRenderer.sprite = I_NormalSprite;
        m_NumberInArray = numberInArray;
        m_Position = position;
        m_TimeOfCreation = Time.fixedTime;
        I_DistanceColliderObject.GetComponent<CircleCollider2D>().enabled = false;
        m_TimeUntilDestory = 0.0f;
        m_HasCheckedForExternalExplodeCollider = false;
        m_ParticleSystem.Stop();
    }
    public void HitColliderCall(GameObject go)
    {
        if (go.layer == GlobalVariables.LayerBullets)
        {
            Explode();
            go.GetComponent<Bullet>().DestroyBullet();
        }
    }
    public void DistanceColliderCall(int layer)
    {
        //Check who collided with
        if (layer == GlobalVariables.LayerTanks)
        {
            if (m_TimeOfCreation > Time.fixedTime - 8.0f + I_TimeDelayForTankNearExposion)
                m_TimeOfCreation = Time.fixedTime - 8.0f + I_TimeDelayForTankNearExposion;
        }
    }
    public void SetLevelInArray(int level)
    {
        m_NumberInArray = level;
    }
}
