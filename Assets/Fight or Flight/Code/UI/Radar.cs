using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Radar : MonoBehaviour
{
    public float range = 2000f;
    public RectTransform radarContainer;
    public GameObject enemyIconPrefab;
    public GameObject asteroidIconPrefab;
    public float updateInterval = 0.1f;

    private List<RadarIcon> activeIcons = new List<RadarIcon>();
    private Stack<GameObject> enemyIconPool = new Stack<GameObject>();
    private Stack<GameObject> asteroidIconPool = new Stack<GameObject>();
    private float nextUpdateTime;

    private struct RadarIcon
    {
        public Transform target;
        public GameObject icon;
        public bool isEnemy;
    }

    private void Update()
    {
        if (Ship.PlayerShip == null) return;

        if (Time.time >= nextUpdateTime)
        {
            UpdateTargets();
            nextUpdateTime = Time.time + updateInterval;
        }

        UpdateIconPositions();
    }

    private void UpdateTargets()
    {
        // Return active icons to pools
        foreach (var item in activeIcons)
        {
            item.icon.SetActive(false);
            if (item.isEnemy) enemyIconPool.Push(item.icon);
            else asteroidIconPool.Push(item.icon);
        }
        activeIcons.Clear();

        Vector3 playerPos = Ship.PlayerShip.transform.position;

        // Find Enemies
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var enemy in enemies)
        {
            if (Vector3.Distance(enemy.transform.position, playerPos) <= range)
            {
                GameObject icon = GetIcon(true);
                activeIcons.Add(new RadarIcon { target = enemy.transform, icon = icon, isEnemy = true });
            }
        }

        // Find Asteroids
        Asteroid[] asteroids = Object.FindObjectsByType<Asteroid>(FindObjectsInactive.Exclude);
foreach (var asteroid in asteroids)
        {
            if (Vector3.Distance(asteroid.transform.position, playerPos) <= range)
            {
                GameObject icon = GetIcon(false);
                activeIcons.Add(new RadarIcon { target = asteroid.transform, icon = icon, isEnemy = false });
            }
        }
    }

    private GameObject GetIcon(bool isEnemy)
    {
        Stack<GameObject> pool = isEnemy ? enemyIconPool : asteroidIconPool;
        GameObject prefab = isEnemy ? enemyIconPrefab : asteroidIconPrefab;

        if (pool.Count > 0)
        {
            GameObject icon = pool.Pop();
            icon.SetActive(true);
            return icon;
        }

        GameObject newIcon = Instantiate(prefab, radarContainer);
        newIcon.SetActive(true);
        return newIcon;
    }

    private void UpdateIconPositions()
    {
        Vector3 playerPos = Ship.PlayerShip.transform.position;
        Quaternion playerRot = Ship.PlayerShip.transform.rotation;
        float scale = radarContainer.rect.width * 0.5f / range;

        for (int i = activeIcons.Count - 1; i >= 0; i--)
        {
            var item = activeIcons[i];
            if (item.target == null)
            {
                item.icon.SetActive(false);
                activeIcons.RemoveAt(i);
                continue;
            }

            Vector3 diff = item.target.position - playerPos;
            Vector3 localPos = Quaternion.Inverse(playerRot) * diff;
            
            Vector2 uiPos = new Vector2(localPos.x, localPos.z) * scale;
            item.icon.GetComponent<RectTransform>().anchoredPosition = uiPos;
        }
    }
}
