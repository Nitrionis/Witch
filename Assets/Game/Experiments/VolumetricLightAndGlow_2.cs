using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Assets.Game.Tools;

namespace Assets.Game.Experiments
{
	public class VolumetricLightAndGlow_2 : MonoBehaviour
	{
		private Camera camera;

		private void Start()
		{
			
		}

		private void Update()
		{
			//var cameraLeftDownWS = camera.Get
			Vector3[] corners = new Vector3[4];
			float distance = Mathf.Lerp(camera.nearClipPlane, camera.farClipPlane, 0.5f);

			// Bottom-left corner
			corners[0] = camera.ViewportToWorldPoint(new Vector3(0, 0, distance));

			// Bottom-right corner
			corners[1] = camera.ViewportToWorldPoint(new Vector3(1, 0, distance));

			// Top-left corner
			corners[2] = camera.ViewportToWorldPoint(new Vector3(0, 1, distance));

			// Top-right corner
			corners[3] = camera.ViewportToWorldPoint(new Vector3(1, 1, distance));
		}

		public struct Rectangle
		{
			public float3 DawnLeftWS;
			public float3 UpLeftWS;
			public float3 UpRightWS;
			public float3 DawnRightWS;
			public int Shadow1;
			public int Shadow2;
			public int Shadow3;
			public int Shadow4;
			public int Shadow5;
			public int Shadow6;
			public int Shadow7;
			public int Shadow8;
			public int Shadow9;
		}

		private class MeshController : IDisposable
		{
			public Rectangle[] Rectangles;

			private readonly Mesh mesh;
			private readonly MeshFilter meshFilter;

			private NativeArray<Rectangle> nativeRectangles;
			private NativeArray<float4> vertices;
			private NativeArray<int> triangles;
			private NativeArray<float4> normals;
			private NativeArray<float4> colors;

			private bool isInitialized = false;
			private int currentCapacity = 0;

			public MeshController(GameObject gameObject)
			{
				meshFilter = gameObject.GetComponent<MeshFilter>();
				if (meshFilter == null)
					meshFilter = gameObject.AddComponent<MeshFilter>();

				mesh = new Mesh();
				mesh.name = "BurstRectangleMesh";
				mesh.MarkDynamic();
				meshFilter.mesh = mesh;
				isInitialized = true;

				currentCapacity = 64;
				PreAllocateNativeArrays(currentCapacity);
			}

			private void PreAllocateNativeArrays(int capacity)
			{
				DisposeNativeArrays();

				int vertexCapacity = capacity * 4;
				int triangleCapacity = capacity * 6;

				nativeRectangles = new NativeArray<Rectangle>(capacity, Allocator.Persistent);
				vertices = new NativeArray<float4>(vertexCapacity, Allocator.Persistent);
				triangles = new NativeArray<int>(triangleCapacity, Allocator.Persistent);
				normals = new NativeArray<float4>(vertexCapacity, Allocator.Persistent);
				colors = new NativeArray<float4>(vertexCapacity, Allocator.Persistent);
			}

			public void RebuildMeshJobified()
			{
				if (!isInitialized || Rectangles == null || Rectangles.Length == 0) {
					if (mesh != null) mesh.Clear();
					return;
				}

				int rectangleCount = Rectangles.Length;

				// Ensure capacity
				if (rectangleCount > currentCapacity) {
					currentCapacity = Mathf.NextPowerOfTwo(rectangleCount);
					PreAllocateNativeArrays(currentCapacity);
				}

				// Copy rectangles to native array
				for (int i = 0; i < rectangleCount; i++) {
					nativeRectangles[i] = Rectangles[i];
				}

				// Process mesh data (this can be converted to a Burst job)
				for (int rectIndex = 0; rectIndex < rectangleCount; rectIndex++) {
					int vertexBaseIndex = rectIndex * 4;
					int triangleBaseIndex = rectIndex * 6;

					Rectangle rect = nativeRectangles[rectIndex];

					// Vertices
					vertices[vertexBaseIndex] = rect.DawnLeftWS.WithW(rect.Shadow1);
					vertices[vertexBaseIndex + 1] = rect.UpLeftWS.WithW(rect.Shadow1);
					vertices[vertexBaseIndex + 2] = rect.UpRightWS.WithW(rect.Shadow1);
					vertices[vertexBaseIndex + 3] = rect.DawnRightWS.WithW(rect.Shadow1);

					// Triangles
					triangles[triangleBaseIndex] = vertexBaseIndex;
					triangles[triangleBaseIndex + 1] = vertexBaseIndex + 1;
					triangles[triangleBaseIndex + 2] = vertexBaseIndex + 2;
					triangles[triangleBaseIndex + 3] = vertexBaseIndex + 2;
					triangles[triangleBaseIndex + 4] = vertexBaseIndex + 3;
					triangles[triangleBaseIndex + 5] = vertexBaseIndex;

					float4 normal = new float4(
						rect.Shadow2,
						rect.Shadow3,
						rect.Shadow4,
						rect.Shadow5
					);
					normals[vertexBaseIndex] = normal;
					normals[vertexBaseIndex + 1] = normal;
					normals[vertexBaseIndex + 2] = normal;
					normals[vertexBaseIndex + 3] = normal;

					float4 color = new float4(
						rect.Shadow6,
						rect.Shadow7,
						rect.Shadow8,
						rect.Shadow9
					);
					colors[vertexBaseIndex] = color;
					colors[vertexBaseIndex + 1] = color;
					colors[vertexBaseIndex + 2] = color;
					colors[vertexBaseIndex + 3] = color;
				}

				// Apply to mesh
				ApplyMeshDataDirect(rectangleCount * 4, rectangleCount * 6);
			}

			private void ApplyMeshDataDirect(int vertexCount, int triangleCount)
			{
				using var meshDataArray = Mesh.AllocateWritableMeshData(1);
				var meshData = meshDataArray[0];

				meshData.SetVertexBufferParams(vertexCount, new VertexAttributeDescriptor[] {
					new VertexAttributeDescriptor(VertexAttribute.Position),
					new VertexAttributeDescriptor(VertexAttribute.Normal),
					new VertexAttributeDescriptor(VertexAttribute.Color)
				});

				meshData.SetIndexBufferParams(triangleCount, IndexFormat.UInt32);

				// Copy vertex data
				var vertexData = meshData.GetVertexData<MeshVertex>();
				for (int i = 0; i < vertexCount; i++) {
					vertexData[i] = new MeshVertex {
						position = vertices[i],
						normal = normals[i],
						color = colors[i]
					};
				}

				// Copy index data
				var indexData = meshData.GetIndexData<int>();
				for (int i = 0; i < triangleCount; i++) {
					indexData[i] = triangles[i];
				}

				meshData.subMeshCount = 1;
				meshData.SetSubMesh(0, new SubMeshDescriptor(0, triangleCount));

				Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh, MeshUpdateFlags.DontRecalculateBounds);
				//mesh.RecalculateBounds();
			}

			private void DisposeNativeArrays()
			{
				if (nativeRectangles.IsCreated)
					nativeRectangles.Dispose();
				if (vertices.IsCreated)
					vertices.Dispose();
				if (triangles.IsCreated)
					triangles.Dispose();
				if (normals.IsCreated)
					normals.Dispose();
				if (colors.IsCreated)
					colors.Dispose();
			}

			/// <summary>
			/// Always Dispose manualy from OnDestroy()
			/// </summary>
			public void Dispose()
			{
				isInitialized = false;
				DisposeNativeArrays();
			}

			private struct MeshVertex
			{
				public float4 position;
				public float4 normal;
				public float4 color;
			}
		}

		private struct LineSegmentProjection
		{
			private Camera targetCamera;

			public LineSegmentProjection(Camera camera) => targetCamera = camera;

			public (Vector2 StartWS, Vector2 EndWS)? Get(Vector3 startWS, Vector3 endWS)
			{
				float nearClip = targetCamera.nearClipPlane;

				// Use Unity's built-in method to handle the near plane clipping and projection
				Vector3 viewportStart = targetCamera.WorldToViewportPoint(startWS);
				Vector3 viewportEnd = targetCamera.WorldToViewportPoint(endWS);

				// Check if points are behind near plane (z < 0 in viewport space means behind camera)
				bool startBehind = viewportStart.z < 0;
				bool endBehind = viewportEnd.z < 0;

				if (startBehind && endBehind) {
					// Both behind camera - discard
					return null;
				}

				if (!startBehind && !endBehind) {
					// Both in front - use both points
					return (
						new Vector2(viewportStart.x, viewportStart.y),
						new Vector2(viewportEnd.x, viewportEnd.y)
					);
				} else {
					// One point behind, one in front - need to clip
					Vector3 behindPoint = startBehind ? startWS : endWS;
					Vector3 frontPoint = startBehind ? endWS : startWS;

					// Find intersection with near plane
					Vector3 clippedPoint = ClipToNearPlaneWorld(frontPoint, behindPoint, nearClip);
					Vector3 viewportClipped = targetCamera.WorldToViewportPoint(clippedPoint);

					return (
						new Vector2(viewportClipped.x, viewportClipped.y),
						new Vector2(startBehind ? viewportEnd.x : viewportStart.x,
									startBehind ? viewportEnd.y : viewportStart.y)
					);
				}
			}

			private Vector3 ClipToNearPlaneWorld(Vector3 frontPoint, Vector3 backPoint, float nearClip)
			{
				// Direction from back to front point
				Vector3 direction = frontPoint - backPoint;
				float t = (nearClip - Vector3.Dot(backPoint, targetCamera.transform.forward))
						  / Vector3.Dot(direction, targetCamera.transform.forward);

				return backPoint + direction * Mathf.Clamp01(t);
			}
		}
	}
}
