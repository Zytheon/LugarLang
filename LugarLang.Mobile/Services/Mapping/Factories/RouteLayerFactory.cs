using System.Collections.Generic;
using System.Linq;
using CdoGtfsConverter.Models;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using NetTopologySuite.Geometries;

namespace LugarLang.Mobile.Services.Mapping.Factories;

public class RouteLayerFactory
{
    public MemoryLayer CreateLayer(
        IEnumerable<GeoPoint> path,
        Mapsui.Styles.Color lineColor)
    {
        var coordinates = path
            .Select(p =>
            {
                var projected = SphericalMercator.FromLonLat(
                    p.Longitude,
                    p.Latitude);

                return new Coordinate(
                    projected.x,
                    projected.y);
            })
            .ToArray();

        var lineString = new LineString(coordinates);

        var feature = new GeometryFeature
        {
            Geometry = lineString
        };

        feature.Styles.Add(new VectorStyle
        {
            Line = new Pen
            {
                Color = lineColor,
                Width = 6
            }
        });

        var layer = new MemoryLayer
        {
            Name = "Route Layer",
            Features = new List<IFeature>
            {
                feature
            }
        };

        return layer;
    }

    public MemoryLayer CreateStopLayer(
        IEnumerable<Stop> stops)
    {
        var features = new List<IFeature>();

        foreach (Stop stop in stops)
        {
            if (!stop.Latitude.HasValue ||
                !stop.Longitude.HasValue)
            {
                continue;
            }

            var projected = SphericalMercator.FromLonLat(
                stop.Longitude.Value,
                stop.Latitude.Value);

            var point =
                new NetTopologySuite.Geometries.Point(
                    projected.x,
                    projected.y);

            var feature = new GeometryFeature
            {
                Geometry = point
            };

            feature.Styles.Add(new SymbolStyle
            {
                SymbolScale = 0.5,

                Fill = new Mapsui.Styles.Brush(
                    Mapsui.Styles.Color.Red),

                Outline = new Pen(
                    Mapsui.Styles.Color.White,
                    2)
            });

            features.Add(feature);
        }

        return new MemoryLayer
        {
            Name = "Stop Layer",
            Features = features
        };
    }
}


