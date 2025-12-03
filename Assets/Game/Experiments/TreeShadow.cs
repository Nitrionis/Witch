using System;
using UnityEngine;

public class TreeShadow : MonoBehaviour
{
	public Transform dirLight;
	public GameObject cude;

	private Bounds bounds;
	private Vector3 center;
	private Vector3 forwardLight;

	private double y1;
	private double y2;
	private double y3;

	private double r1;
	private double r2;
	private double r3;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
		if (dirLight != null) {
			forwardLight = dirLight.forward;
		} 
		var mr = transform.Find("default").GetComponent<Renderer>();
		bounds = mr.bounds;
		center = bounds.center;
		center.y = 0;

		double h = 1.8 * bounds.extents.y;
		//double thetaRadians = thetaDegrees * Math.PI / 180.0;

		// Calculate the half-base of the triangle
		//double halfBase = h * Math.Tan(thetaRadians / 2);
		double halfBase = Math.Max(bounds.extents.x, bounds.extents.z);

		// Calculate the radii and positions of the three circles
		(y1, r1) = CalculateTopCircle(h, halfBase);
		(y2, r2) = CalculateMiddleCircle(h, halfBase, y1, r1);
		(y3, r3) = CalculateBottomCircle(h, halfBase, y2, r2);

		Debug.Log($"Circle 1: Y = {y1}, Radius = {r1}");
		Debug.Log($"Circle 2: Y = {y2}, Radius = {r2}");
		Debug.Log($"Circle 3: Y = {y3}, Radius = {r3}");

		SpawnCubes();
	}

    // Update is called once per frame
    void Update()
    {
		//DrawBounds(bounds);
		//DrawShadowsBBox();
	}

	private void SpawnCubes()
	{
		var p1 = center + Vector3.up * (float)y1;
		var p2 = center + Vector3.up * (float)y2;
		var p3 = center + Vector3.up * (float)y3;

		SpawnCube(p3, (float)r1 * 2f);
		SpawnCube(p1, (float)r2 * 2f);
		SpawnCube(p2, (float)r3 * 2f);

		void SpawnCube(Vector3 spawnPoint, float r)
		{
			const float len = 7;
			var point = spawnPoint + dirLight.forward * len / 2;
			var obj = Instantiate(cude, point, Quaternion.LookRotation(dirLight.forward));
			obj.transform.localScale = new Vector3(r, r, len);
			var renderer = obj.GetComponent<MeshRenderer>();
			var propertyBlock = new MaterialPropertyBlock();
			propertyBlock.SetFloat("_CapsuleRadius", r * 0.5f);
			propertyBlock.SetVector("_CapsulePointA", spawnPoint + dirLight.forward * r * 0.5f);
			propertyBlock.SetVector("_CapsulePointB", spawnPoint + dirLight.forward * (len - r * 0.5f));
			renderer.SetPropertyBlock(propertyBlock);

			var filter = obj.GetComponent<MeshFilter>();
			var mesh = filter.mesh;
			var vertices = mesh.vertices;
			var normals = mesh.normals;
			for (int i = 0; i < vertices.Length; i++) {
				var v = vertices[i];
				normals[i] = v.z > 0 ? Vector3.zero : Vector3.one;
			}
			mesh.SetNormals(normals);
			filter.mesh = mesh;
		}
	}

	private void DrawShadowsBBox()
	{

		var p1 = center + Vector3.up * (float)y1;
		var p2 = center + Vector3.up * (float)y2;
		var p3 = center + Vector3.up * (float)y3;

		const float len = 4;
		//Debug.DrawLine(p1, p1 + forwardLight * len, Color.blue, 0);
		//Debug.DrawLine(p2, p2 + forwardLight * len, Color.red, 0);
		//Debug.DrawLine(p3, p3 + forwardLight * len, Color.green, 0);

		Vector3 customLeft = Vector3.Cross(forwardLight, Vector3.up).normalized;
		Vector3 customUp = Vector3.Cross(forwardLight, customLeft).normalized;

		DrawRects(p3, (float)r1, Color.blue);
		DrawRects(p1, (float)r2, Color.red);
		DrawRects(p2, (float)r3, Color.green);

		void DrawRects(Vector3 point, float r, Color color)
		{
			Vector3 a1 = point - r * customLeft - r * customUp;
			Vector3 a2 = point - r * customLeft + r * customUp;
			Vector3 a3 = point + r * customLeft + r * customUp;
			Vector3 a4 = point + r * customLeft - r * customUp;

			Vector3 b1 = a1 + forwardLight * len;
			Vector3 b2 = a2 + forwardLight * len;
			Vector3 b3 = a3 + forwardLight * len;
			Vector3 b4 = a4 + forwardLight * len;

			Debug.DrawLine(a1, a2, color, 0);
			Debug.DrawLine(a2, a3, color, 0);
			Debug.DrawLine(a3, a4, color, 0);
			Debug.DrawLine(a4, a1, color, 0);

			Debug.DrawLine(b1, b2, color, 0);
			Debug.DrawLine(b2, b3, color, 0);
			Debug.DrawLine(b3, b4, color, 0);
			Debug.DrawLine(b4, b1, color, 0);

			Debug.DrawLine(a1, b1, color, 0);
			Debug.DrawLine(a2, b2, color, 0);
			Debug.DrawLine(a3, b3, color, 0);
			Debug.DrawLine(a4, b4, color, 0);

		}
	}

	private void DrawBounds(Bounds b, float delay = 0)
	{
		// bottom
		var p1 = new Vector3(b.min.x, b.min.y, b.min.z);
		var p2 = new Vector3(b.max.x, b.min.y, b.min.z);
		var p3 = new Vector3(b.max.x, b.min.y, b.max.z);
		var p4 = new Vector3(b.min.x, b.min.y, b.max.z);

		Debug.DrawLine(p1, p2, Color.blue, delay);
		Debug.DrawLine(p2, p3, Color.red, delay);
		Debug.DrawLine(p3, p4, Color.yellow, delay);
		Debug.DrawLine(p4, p1, Color.magenta, delay);

		// top
		var p5 = new Vector3(b.min.x, b.max.y, b.min.z);
		var p6 = new Vector3(b.max.x, b.max.y, b.min.z);
		var p7 = new Vector3(b.max.x, b.max.y, b.max.z);
		var p8 = new Vector3(b.min.x, b.max.y, b.max.z);

		Debug.DrawLine(p5, p6, Color.blue, delay);
		Debug.DrawLine(p6, p7, Color.red, delay);
		Debug.DrawLine(p7, p8, Color.yellow, delay);
		Debug.DrawLine(p8, p5, Color.magenta, delay);

		// sides
		Debug.DrawLine(p1, p5, Color.white, delay);
		Debug.DrawLine(p2, p6, Color.gray, delay);
		Debug.DrawLine(p3, p7, Color.green, delay);
		Debug.DrawLine(p4, p8, Color.cyan, delay);
	}

	private static (double y, double r) CalculateTopCircle(
		double h, double halfBase
		)
	{
		// The top circle is tangent to the two sides and the 'top' of the triangle (but not really, it's the incircle?)
		// For a triangle centered at Y-axis with apex at (0,h), base from (-halfBase,0) to (halfBase,0)
		// The largest circle that fits is the incircle. But since it's placed along Y-axis, perhaps not.
		// Alternatively, the top circle's radius can be derived from the angle.
		// The formula for the radius of the top circle in such a configuration is:
		double r1 = (h * halfBase) / (halfBase + Math.Sqrt(h * h + halfBase * halfBase));
		double y1 = h - r1;
		return (y1, r1);
	}

	private static (double y, double r) CalculateMiddleCircle(
		double h, double halfBase, double yAbove, double rAbove
		)
	{
		// The next circle is tangent to the sides of the triangle and the circle above
		// The distance between the centers is r1 + r2
		// The new circle's center is at y2 = y1 - r1 - r2 - ... or something else?
		// The line forming the side of the triangle has equation: (y - h)/x = (0 - h)/(-halfBase - 0) => y = h + (h/halfBase)*x for left side
		// The distance from (0, y2) to the line is r2
		// The line equation can be rewritten as (h/halfBase)x - y + h =0
		// Distance from (0, y2) to the line is |0 - y2 + h| / sqrt((h/halfBase)^2 + 1) = r2
		// So (h - y2) / sqrt( (h^2 + halfBase^2)/halfBase^2 ) = r2
		// => (h - y2) * halfBase / sqrt(h^2 + halfBase^2) = r2
		// Also, the vertical distance between yAbove and y2 is rAbove + r2
		// So yAbove - y2 = rAbove + r2 => y2 = yAbove - rAbove - r2
		// Substitute y2 into the previous equation:
		// (h - (yAbove - rAbove - r2)) * halfBase / sqrt(h^2 + halfBase^2) = r2
		// Let k = halfBase / sqrt(h^2 + halfBase^2)
		// Then (h - yAbove + rAbove + r2) *k = r2
		// => (h - yAbove + rAbove) *k + k r2 = r2
		// => (h - yAbove + rAbove)*k = r2 (1 -k)
		// => r2 = k (h - yAbove + rAbove) / (1 -k)
		double sqrtDenominator = Math.Sqrt(h * h + halfBase * halfBase);
		double k = halfBase / sqrtDenominator;
		double numerator = k * (h - yAbove + rAbove);
		double denominator = 1 - k;
		double r2 = numerator / denominator;
		double y2 = yAbove - rAbove - r2;
		return (y2, r2);
	}

	private static (double y, double r) CalculateBottomCircle(
		double h, double halfBase, double yAbove, double rAbove
		)
	{
		// Similar to the middle circle, but now placed below the middle circle
		double sqrtDenominator = Math.Sqrt(h * h + halfBase * halfBase);
		double k = halfBase / sqrtDenominator;
		double numerator = k * (h - yAbove + rAbove);
		double denominator = 1 - k;
		double r3 = numerator / denominator;
		double y3 = yAbove - rAbove - r3;

		// Check if the bottom circle's radius extends below 0
		if (y3 - r3 < 0) {
			r3 = yAbove - rAbove; // The bottom circle touches the base
			y3 = r3;
			// Recalculate r3 considering it's tangent to the base
			// The distance from (0, y3) to the base (y=0) is y3 = r3
			// Also, the distance to the side is r3: (h - y3) *k = r3
			// => (h - r3)*k = r3 => hk = r3 (1 +k)
			// => r3 = hk / (1 +k)
			r3 = (h * k) / (1 + k);
			y3 = r3;
		}
		return (y3, r3);
	}
}
