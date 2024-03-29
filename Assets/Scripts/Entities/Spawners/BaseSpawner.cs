using UnityEngine;

public class BaseSpawner : MonoBehaviour, ISpawner
{
    [Tooltip("Prefab to spawn")]
    [SerializeField] private GameObject _prefab;

    [Tooltip("How many prefabs to spawn")]
    [SerializeField] private int _howMany;

    [Tooltip("The location to spawn the prefab")]
    [SerializeField] private Transform _spawnLocation;
    public GameObject Prefab => _prefab;

    public void TrySpawn()
    {
        int i;
        for (i = 0; i < _howMany; ++i)
        {
            Instantiate(Prefab, _spawnLocation); //sets _spawnLocation as the parent transform of prefab
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        TrySpawn();
    }

    private void OnDrawGizmos()
    {
        if (Prefab == null)
            return;

        if (!Prefab.TryGetComponent<SpriteRenderer>(out var spriteRenderer)) // note the out var!
            return;

        var spriteRect = new Rect(transform.position, spriteRenderer.bounds.size);
        var spriteTexture = spriteRenderer.sprite.texture;

        Graphics.DrawTexture(spriteRect, spriteTexture);
    }
}
