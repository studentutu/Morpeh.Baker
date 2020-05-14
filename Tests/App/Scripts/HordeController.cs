using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HordeController : MonoBehaviour
{
    [SerializeField] private List<GameObject> objects = new List<GameObject>();
    private int active = 0;
    public int ActiveOnes => active;
    public int Count => objects.Count;

    public void ChangeNumberTo(int number)
    {
        if (active < number)
        {
            for (int i = 0; i < number; i++)
            {
                objects[i].SetActive(true);
            }
        }
        else
        {
            foreach (var item in objects)
            {
                item.SetActive(false);
            }
            for (int i = 0; i < number; i++)
            {
                objects[i].SetActive(true);
            }
        }
    }
}
