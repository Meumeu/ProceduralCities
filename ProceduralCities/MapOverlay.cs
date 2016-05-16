using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProceduralCities
{
	public class MapOverlay : MonoBehaviour
	{
		CelestialBody _body;
		MeshRenderer meshRenderer;
		Mesh mesh;

		public CelestialBody Body
		{
			set
			{
				_body = value;

				gameObject.layer = value.scaledBody.gameObject.layer;
				gameObject.transform.parent = value.scaledBody.transform;
				gameObject.transform.localScale = Vector3d.one * 1.001 * _body.Radius * ScaledSpace.InverseScaleFactor * 10;
				gameObject.transform.localPosition = Vector3.zero;
				gameObject.transform.localRotation = Quaternion.identity;
			}
			get
			{
				return _body;
			}
		}

		bool _Visible = true;
		public bool Visible
		{
			get
			{
				return _Visible;
			}
			set
			{
				_Visible = value;
				OnMapVisibility();
			}
		}

		protected void Awake()
		{
			var meshFilter = gameObject.AddComponent<MeshFilter>();
			meshRenderer = gameObject.AddComponent<MeshRenderer>();
			mesh = meshFilter.mesh;

			meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			meshRenderer.receiveShadows = false;

			var material = new Material(Shader.Find("Sprite/Vertex Colored"));
			meshRenderer.material = material;

			GameEvents.OnMapEntered.Add(MapEntered);
			GameEvents.OnMapExited.Add(MapExited);
			GameEvents.onLevelWasLoaded.Add(LevelWasLoaded);

			OnMapVisibility();
		}

		protected void OnDestroy()
		{
			GameEvents.OnMapEntered.Remove(MapEntered);
			GameEvents.OnMapExited.Remove(MapExited);
			GameEvents.onLevelWasLoaded.Remove(LevelWasLoaded);
		}

		void LevelWasLoaded(GameScenes scene)
		{
			OnMapVisibility();
		}

		void MapEntered()
		{
			try
			{
				if (meshRenderer != null)
					meshRenderer.enabled = _Visible;
			}
			catch
			{
			}
		}

		void MapExited()
		{
			try
			{
				if (meshRenderer != null)
					meshRenderer.enabled = false;
			}
			catch
			{
			}
		}

		void OnMapVisibility()
		{
			try
			{
				if (meshRenderer != null)
					meshRenderer.enabled = Visible && HighLogic.LoadedSceneHasPlanetarium && MapView.MapIsEnabled;
			}
			catch
			{
			}
		}

		protected void UpdateMesh(IEnumerable<Vector3> vertices, IEnumerable<int> triangles, IEnumerable<Color32> colors)
		{
			UpdateMesh(vertices.ToArray(), triangles.ToArray(), colors.ToArray());
		}

		protected void UpdateMesh(Vector3[] vertices, int[] triangles, Color32[] colors)
		{
			mesh.Clear();

			mesh.vertices = vertices;
			mesh.colors32 = colors;
			mesh.triangles = triangles;

			mesh.RecalculateBounds();
			mesh.Optimize();
			OnMapVisibility();
		}
	}
}

