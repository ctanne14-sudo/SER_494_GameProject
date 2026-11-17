using UnityEngine;

public class TutorialHints : MonoBehaviour
{
    private FloorGuide guide;
    private Vector3 startPosition;
    private bool hasStart;
    private float shownTime;

    void Start()
    {
        guide = GetComponent<FloorGuide>();
    }

    void Update()
    {
        if (guide.IsHidden() || guide.Player() == null) return;

        if (!hasStart)
        {
            startPosition = guide.Player().position;
            hasStart = true;
        }

        if (guide.IsHintShowing()) return;

        if (!Seen("Move"))
        {
            guide.ShowHint("Use W A S D to move around", 6f);
            MarkSeen("Move");
            return;
        }

        if (!Seen("Menu") && Vector3.Distance(startPosition, guide.Player().position) > 2f)
        {
            guide.ShowHint("Press Esc for the menu: status, combos and equipment", 6f);
            MarkSeen("Menu");
            return;
        }

        if (!Seen("Inventory") && Seen("Menu"))
        {
            guide.ShowHint("Press Tab for your inventory. Keys 1 - 5 pick hotbar items", 6f);
            MarkSeen("Inventory");
            return;
        }

        if (!Seen("Map") && Seen("Inventory"))
        {
            guide.ShowHint("Press M to open the full floor map", 6f);
            MarkSeen("Map");
            return;
        }

        float enemyDistance = guide.DistanceTo(guide.ClosestEnemy());
        if (!Seen("Enemy") && enemyDistance >= 0f && enemyDistance < 10f)
        {
            guide.ShowHint("Walk into an enemy to start a battle", 6f);
            MarkSeen("Enemy");
            return;
        }

        float exitDistance = guide.DistanceTo(guide.Exit());
        if (!Seen("Exit") && guide.EnemiesLeft() == 0 && exitDistance >= 0f && exitDistance < 8f)
        {
            guide.ShowHint("Walk into the exit to reach the next floor", 6f);
            MarkSeen("Exit");
        }
    }

    bool Seen(string hint)
    {
        return PlayerPrefs.GetInt("Hint_" + hint, 0) == 1;
    }

    void MarkSeen(string hint)
    {
        PlayerPrefs.SetInt("Hint_" + hint, 1);
        PlayerPrefs.Save();
    }

    public static void ResetHints()
    {
        foreach (string hint in new string[] { "Move", "Menu", "Inventory", "Map", "Enemy", "Exit" })
        {
            PlayerPrefs.DeleteKey("Hint_" + hint);
        }
        PlayerPrefs.Save();
    }
}
