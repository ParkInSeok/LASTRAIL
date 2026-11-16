using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
namespace LastRail.Qa {
 // Editor-only assembly. Geometry is pinned TEST_ONLY lab input, not a production map catalog.
 public sealed class MapGrayboxFixtureGeometry {
  public static readonly string Root=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../QA/MapGraybox-20260914"));
  public readonly struct Point {public readonly double X,Y;public Point(double x,double y){X=x;Y=y;}}
  readonly Dictionary<string,Point[][]> modules=new Dictionary<string,Point[][]>();
  public string[] Ids=>modules.Keys.ToArray();
  public static string Hash(string path){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
  public MapGrayboxFixtureGeometry(){
   var manifest=JObject.Parse(File.ReadAllText(Path.Combine(Root,"manifest.json")));
   if((string)manifest["version"]!="graybox-fixture-v1"||manifest["modules"] is not JArray rows||rows.Count!=6)throw new FormatException("Six fixture modules required");
   foreach(var row in rows){var id=(string)row["id"];if(!new[]{"SAFE_STRAIGHT","DEBRIS_SINGLE","BATTERY_SINGLE","FORK_DEBRIS","FORK_ROCKFALL","FORK_BATTERY"}.Contains(id))throw new FormatException();
    string path=Path.Combine(Root,"modules",id+".json");if((string)row["json"]!=id+".json"||Hash(path)!=(string)row["sha256"])throw new FormatException("Geometry pin mismatch");
    var data=JObject.Parse(File.ReadAllText(path));if((string)data["id"]!=id)throw new FormatException();var paths=(JArray)data["paths"];
    if(paths.Count!=(id.StartsWith("FORK_")?2:1))throw new FormatException();var parsed=new List<Point[]>();
    foreach(var route in paths){var points=(JArray)route["points"];if(points.Count!=121)throw new FormatException();var values=new Point[121];for(int i=0;i<121;i++){var p=(JArray)points[i];if(p.Count!=2||p.Any(x=>x.Type!=JTokenType.Integer)||(long)p[0]!=i*5000L||Math.Abs((long)p[1])>180000)throw new FormatException();values[i]=new Point((long)p[0],(long)p[1]);}if(values[0].Y!=0||values[120].Y!=0||Math.Abs(Length(values)-(double)route["lengthMm"])>.000001)throw new FormatException("Socket/length mismatch");parsed.Add(values);}
    modules.Add(id,parsed.ToArray());
   }
  }
  public Point[] PathFor(string id,int route){if(!modules.TryGetValue(id,out var routes)||route<0||route>=routes.Length)throw new ArgumentException("Invalid fixture route");return (Point[])routes[route].Clone();}
  public Point[] Assemble(string[] ids,int[] choices){if(ids==null||choices==null||ids.Length==0||ids.Length!=choices.Length)throw new ArgumentException();var points=new List<Point>();for(int i=0;i<ids.Length;i++){var route=PathFor(ids[i],choices[i]);for(int j=i==0?0:1;j<route.Length;j++)points.Add(new Point(route[j].X+i*600000d,route[j].Y));}return points.ToArray();}
  public static double Length(Point[] points){double distance=0;for(int i=1;i<points.Length;i++){double dx=points[i].X-points[i-1].X,dy=points[i].Y-points[i-1].Y;distance+=Math.Sqrt(dx*dx+dy*dy);}return distance;}
  public static Point At(Point[] points,double distance){if(double.IsNaN(distance)||double.IsInfinity(distance)||points==null||points.Length<2)throw new ArgumentException();if(distance<0)return new Point(points[0].X+distance,points[0].Y);for(int i=1;i<points.Length;i++){var a=points[i-1];var b=points[i];double dx=b.X-a.X,dy=b.Y-a.Y,n=Math.Sqrt(dx*dx+dy*dy);if(distance<=n){double t=n==0?0:distance/n;return new Point(a.X+dx*t,a.Y+dy*t);}distance-=n;}return new Point(points[points.Length-1].X+distance,points[points.Length-1].Y);}
 }
}
