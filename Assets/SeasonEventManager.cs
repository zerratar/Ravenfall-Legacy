using System;
using System.Linq;
using UnityEngine;

public class SeasonEventManager : MonoBehaviour
{
    public SeasonalEvent[] Events;

    // Update is called once per frame
    void Update()
    {
        if (Events == null || Events.Length == 0)
        {
            return;
        }

        // use local time
        var now = DateTime.Now;

        // if we take the event with least days first and get the first date range matchin our current date
        // then we can ensure we select a unique event in the middle of a different event. For instance, lets say we call
        // the default state an event so we can replace certain objects when another event is active, the default one is set to
        // first of jan to last of dec, so this would always apply. Now if we want halloween to happen, we must make sure to pick events
        // with least days first since it will only be 31 days long, this ensures we will have halloween. And if we have a unique day
        // during halloween, that one will also be correctly selected.
        var targetEvent = Events.OrderBy(x => x.DateRange.Days).FirstOrDefault(x => now >= x.DateRange.Start && now <= x.DateRange.End);

        foreach (var evt in Events)
        {
            if (targetEvent == evt) continue; // no need to flicker our objects in the scene.
            Deactivate(evt);
        }

        Activate(targetEvent);
    }

    private void Deactivate(SeasonalEvent evt)
    {
        foreach (var obj in evt.Objects)
        {
            obj.SetActive(false);
        }
    }

    private void Activate(SeasonalEvent evt)
    {
        foreach (var obj in evt.Objects)
        {
            obj.SetActive(true);
        }
    }
}

[Serializable]
public class SeasonalEvent
{
    public string Name;
    public GameObject[] Objects;
    public SeasonalDateRange DateRange;
}

[Serializable]
public class SeasonalDateRange
{
    private DateRange lastRange;
    private string lastRangeString;

    public string Range;

    public DateRange DateRange => Parse(Range);
    public int Days => DateRange.Days;
    public DateTime Start => DateRange.Start;
    public DateTime End => DateRange.End;

    private DateRange Parse(string range)
    {
        if (lastRangeString == range) return lastRange;

        string[] rangeValues = null;
        if (range.Contains("=>"))
        {
            rangeValues = range.Split("=>").Select(x => x.Trim()).ToArray();
        }
        else if (range.Contains(","))
        {
            rangeValues = range.Split(",").Select(x => x.Trim()).ToArray();
        }

        lastRangeString = range;
        lastRange = new DateRange
        {
            Start = ParseDate(rangeValues[0]),
            End = ParseDate(rangeValues[1])
        };

        return lastRange;
    }

    private DateTime ParseDate(string v)
    {
        var thisYear = DateTime.Now.Year;
        var monthDay = v.Split('-');
        return new DateTime(thisYear, int.Parse(monthDay[0]), int.Parse(monthDay[1]));
    }
}