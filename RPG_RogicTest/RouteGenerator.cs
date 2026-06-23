using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class RouteGenerator(IRandomProvider randomProvider)
{
    private readonly IRandomProvider _random = randomProvider;
    private readonly RouteContentGenerator _contentGenerator = new RouteContentGenerator(randomProvider);

    public List<RouteData> CreateRoutes(FloorData floorData)
    {
        int routeAmount = _random.GetRandomInt(1, 4);
        List<RouteData> result = new();
        var directions = ShuffleDirection();
        for(int i = 0; i < routeAmount; i++)
        {
            var routeData = CreateRoute(floorData, directions[i]);
            result.Add(routeData);
        }
        return result;
    }
    private RouteData CreateRoute(FloorData floorData, DirectionType direction)
    {
        int total = floorData.EventRate.Sum(data => data.Item2);
        int rdm = _random.GetRandomInt(0, total);
        foreach (var (type, rate) in floorData.EventRate)
        {
            if (rdm < rate)
            {
                int progress = _random.GetRandomInt(3, 16); //マジックナンバー、将来的にフロアデータにいれるかも
                RouteContentData routeContentData = _contentGenerator.CreateEventContent(type, floorData);
                return new RouteData(type, direction, progress, routeContentData);
            }
            rdm -= rate;
        }
        throw new InvalidOperationException();
    }

    private List<DirectionType> ShuffleDirection()
    {
        List<DirectionType> directions = new() { DirectionType.Left, DirectionType.Right, DirectionType.Center };
        int n = directions.Count;
        while(n > 1)
        {
            n--;
            int rdm = _random.GetRandomInt(0, n + 1);
            var value = directions[rdm];
            directions[rdm] = directions[n];
            directions[n] = value;
        }
        return directions;
    }
}

public record RouteData
(
    DungeonEventType EventType,
    DirectionType DirectionType,
    int Progress,
    RouteContentData RouteContentData
);

