using System.Collections.Generic;
using UnityEngine;

public class SeatManager : MonoBehaviour
{
    public static SeatManager Instance;

    public List<Seat> seats = new List<Seat>();

    private void Awake()
    {
        Instance = this;
    }

    public Seat GetFirstAvailableSeat()
    {
        foreach (Seat s in seats)
        {
            if (!s.isOccupied)
                return s;
        }

        return null; // no seats available
    }
}
