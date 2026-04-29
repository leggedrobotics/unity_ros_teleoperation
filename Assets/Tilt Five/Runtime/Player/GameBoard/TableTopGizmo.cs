/*
 * Copyright (C) 2020-2023 Tilt Five, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;

namespace TiltFive
{
	/// <summary>
	/// The gizmo for the Tilt Five game board.
	/// </summary>
	public class TableTopGizmo {

		private string boardMeshAssetPath_LE = MeshAssets.GetPathToGameboardMesh(GameboardType.GameboardType_LE);
		private string surfaceMeshChildPath_LE = "Surface_LE";
		private string borderMeshChildPath_LE = "Border_LE";

		private string boardMeshAssetPath_XE = MeshAssets.GetPathToGameboardMesh(GameboardType.GameboardType_XE);
		private string surfaceMeshChildPath_XE = "Surface_Flat";
		private string borderMeshChildPath_XE = "Border_Flat";

		private string boardMeshAssetPath_XE_Raised = MeshAssets.GetPathToGameboardMesh(GameboardType.GameboardType_XE_Raised);
		private string surfaceMeshChildPath_XE_Raised = "Surface_Raised";
		private string borderMeshChildPath_XE_Raised = "Border_Raised";

		private string logoMeshAssetPath = MeshAssets.GetPathToT5LogoMesh();
		private string logoBorderChildPath = "Circle";
		private string logoLeftCharacterChildPath = "Tilt";
		private string logoRightCharacterChildPath = "Five";

		private float gizmoAlpha;
		private ScaleSettings scaleSettings;
		private GameBoardSettings gameBoardSettings;
		private float scaleToUWRLD_UGBD => scaleSettings.GetScaleToUWRLD_UGBD(gameBoardSettings.gameBoardScale);
		private GameBoard.GameboardExtents gameboardExtents = new GameBoard.GameboardExtents(
			GameBoard.GameboardExtents.GAMEBOARD_SIZE_LE);

		private float totalGameBoardWidthInMeters =>
			usableGameBoardWidthInMeters + 2f * GameBoard.GameboardExtents.BORDER_WIDTH_IN_METERS;
		private float totalGameBoardLengthInMeters =>
			usableGameBoardLengthInMeters + 2f * GameBoard.GameboardExtents.BORDER_WIDTH_IN_METERS;

		private float usableGameBoardWidthInMeters => gameboardExtents.ViewableSpanX.ToMeters;
		private float usableGameBoardLengthInMeters => gameboardExtents.ViewableSpanZ.ToMeters;

		private GameboardType gameboardType;

		private GameObject meshObj_LE;
		private Mesh meshBorder_LE;
		private Mesh meshSurface_LE;

		private GameObject meshObj_XE;
		private Mesh meshBorder_XE;
		private Mesh meshSurface_XE;

		private GameObject meshObj_XE_Raised;
		private Mesh meshBorder_XE_Raised;
		private Mesh meshSurface_XE_Raised;

		private GameObject logoObj;
		private Mesh meshLogoBorder;
		private Mesh meshLogoLeftCharacter;
		private Mesh meshLogoRightCharacter;

		private Mesh meshRuler;
		private List<LineSegment> rulerData;


        // The lines comprising the board gizmo's unit grid.
        private List<LineSegment> xGridLines;
		private List<LineSegment> zGridLines;
		private float yGridOffset;

		private struct LineSegment
        {
            public LineSegment(Vector3 start, Vector3 end, int LOD)
            {
                this.Start = start;
                this.End = end;
				this.LOD = LOD;
            }
            public Vector3 Start;
            public Vector3 End;
			public int LOD;
        }


		private void Configure(ScaleSettings scaleSettings, GameBoardSettings gameBoardSettings,
			float alpha, float gridOffsetY, GameBoard.GameboardExtents gameboardExtents)
		{
			this.scaleSettings = scaleSettings;
			this.gameBoardSettings = gameBoardSettings;
			this.gizmoAlpha = alpha;
			this.yGridOffset = gridOffsetY;
			this.gameboardExtents = gameboardExtents;

			if (meshObj_LE == null || meshBorder_LE == null || meshSurface_LE == null)
			{
				LoadGameboardModel(boardMeshAssetPath_LE, surfaceMeshChildPath_LE, borderMeshChildPath_LE,
					out meshObj_LE, out meshSurface_LE, out meshBorder_LE);
			}
			if (meshObj_XE == null || meshBorder_XE == null || meshSurface_XE == null)
			{
				LoadGameboardModel(boardMeshAssetPath_XE, surfaceMeshChildPath_XE, borderMeshChildPath_XE,
					out meshObj_XE, out meshSurface_XE, out meshBorder_XE);
			}
			if (meshObj_XE_Raised == null || meshBorder_XE_Raised == null || meshSurface_XE_Raised == null)
			{
				LoadGameboardModel(boardMeshAssetPath_XE_Raised, surfaceMeshChildPath_XE_Raised, borderMeshChildPath_XE_Raised,
					out meshObj_XE_Raised, out meshSurface_XE_Raised, out meshBorder_XE_Raised);
			}

			if(null == logoObj)
			{
				logoObj = AssetDatabase.LoadAssetAtPath<GameObject>(logoMeshAssetPath);
			}

			if(null == meshLogoBorder)
			{
				var transform = logoObj.transform.Find( logoBorderChildPath );
				if(transform != null)
				{
					if(transform.TryGetComponent<MeshFilter>(out var meshFilter))
					{
						meshLogoBorder = meshFilter.sharedMesh;
					}
				}
			}
			if(null == meshLogoLeftCharacter)
			{
				var transform = logoObj.transform.Find( logoLeftCharacterChildPath );
				if(transform != null)
				{
					if(transform.TryGetComponent<MeshFilter>(out var meshFilter))
					{
						meshLogoLeftCharacter = meshFilter.sharedMesh;
					}
				}
			}
			if(null == meshLogoRightCharacter)
			{
				var transform = logoObj.transform.Find( logoRightCharacterChildPath );
				if(transform != null)
				{
					if(transform.TryGetComponent<MeshFilter>(out var meshFilter))
					{
						meshLogoRightCharacter = meshFilter.sharedMesh;
					}
				}
			}

			var previousGameboardType = gameboardType;
			gameboardType = gameBoardSettings.currentGameBoard.GetDisplayedGameboardType(gameBoardSettings);

			bool gameboardTypeChanged = previousGameboardType != gameboardType;

			if (null == xGridLines || null == zGridLines || gameboardTypeChanged)
            {
                ResetGrid();
            }

			if(null == meshRuler || gameboardTypeChanged)
			{
				ConstructRulerMesh(gameboardExtents.ViewableSpanX, "meshRuler", out meshRuler);
			}
		}

		public void LoadGameboardModel(string meshAssetPath, string surfaceMeshChildPath, string borderMeshChildPath,
			out GameObject meshObj, out Mesh meshSurface, out Mesh meshBorder)
        {
			meshObj = AssetDatabase.LoadAssetAtPath<GameObject>(meshAssetPath);

			var surfaceMeshTransform = meshObj.transform.Find(surfaceMeshChildPath);
			if (surfaceMeshTransform != null)
			{
				meshSurface = surfaceMeshTransform.TryGetComponent<MeshFilter>(out var meshFilter) ? meshFilter.sharedMesh : null;
			}
			else
            {
				meshSurface = null;
			}

			var borderMeshTransform = meshObj.transform.Find(borderMeshChildPath);
			if (borderMeshTransform != null)
			{
				meshBorder = borderMeshTransform.TryGetComponent<MeshFilter>(out var meshFilter) ? meshFilter.sharedMesh : null;
			}
			else
			{
				meshBorder = null;
			}
		}

		public void ConstructRulerMesh(Length rulerLength, string meshName, out Mesh meshRuler)
        {
			var rulerData = new List<LineSegment>();

			float oneMillimeterLengthInMeters = new Length(1, LengthUnit.Millimeters).ToMeters;

			// For the centimeter ruler, we're going to draw regular marks for centimeters, and smaller ones for millimeters.
			for (int i = 0; i * oneMillimeterLengthInMeters < rulerLength.ToMeters; i++)
			{
				float currentDistance = i * (oneMillimeterLengthInMeters / rulerLength.ToMeters);
				float smallestFractionOfBoardWidth = 1f / 150f;

				float tickMarkLength = smallestFractionOfBoardWidth;
				int lod = 3;

				lod -= i % 5 == 0 ? 1 : 0;
				lod -= i % 10 == 0 ? 1 : 0;

				tickMarkLength += (3 - lod) * smallestFractionOfBoardWidth;

				rulerData.Add(new LineSegment(new Vector3(currentDistance, 0f, 0f), new Vector3(currentDistance, 0f, tickMarkLength), lod));
			}

			float oneSixteenthInchLengthInMeters = new Length(1 / 16f, LengthUnit.Inches).ToMeters;

			// For the inch ruler, we're going to draw regular marks for inches, and smaller ones for half/quarter/eighth/sixteenth inches.
			for (int i = 0; i * oneSixteenthInchLengthInMeters < rulerLength.ToMeters; i++)
			{
				float currentDistance = i * (oneSixteenthInchLengthInMeters / rulerLength.ToMeters);
				float smallestFractionOfBoardWidth = 1f / 300f;

				float tickMarkLength = smallestFractionOfBoardWidth;
				int lod = 5;

				lod -= i % 2 == 0 ? 1 : 0;
				lod -= i % 4 == 0 ? 1 : 0;
				lod -= i % 8 == 0 ? 1 : 0;
				lod -= i % 16 == 0 ? 1 : 0;

				tickMarkLength += (5 - lod) * smallestFractionOfBoardWidth;

				float offsetFromCentimeterRuler = 1 / 16f;
				rulerData.Add(new LineSegment(
					new Vector3(currentDistance, 0f, offsetFromCentimeterRuler - tickMarkLength),
					new Vector3(currentDistance, 0f, offsetFromCentimeterRuler),
					lod));
			}

			meshRuler = new Mesh();
			meshRuler.name = meshName;

			int vertArraySizeRatio = 4;     // There are 4 vertices for every line in rulerData.
			Vector3[] verts = new Vector3[rulerData.Count * vertArraySizeRatio];

			int triArraySizeRatio = 6;      // There are 6 triangle vertex indices for every line in rulerData.
			int[] triangles = new int[rulerData.Count * triArraySizeRatio];

			float lineThickness = 1 / 2800f;

			// We want to offset the x vector component to achieve line thickness.
			var lineThicknessOffset = Vector3.right * (lineThickness / 2f);

			for (int i = 0; i < rulerData.Count; i++)
			{
				var line = rulerData[i];

				var bottomLeft = line.Start - lineThicknessOffset + Vector3.left / 2 + Vector3.back / 32f;
				var topLeft = line.End - lineThicknessOffset + Vector3.left / 2 + Vector3.back / 32f;

				var bottomRight = line.Start + lineThicknessOffset + Vector3.left / 2 + Vector3.back / 32f;
				var topRight = line.End + lineThicknessOffset + Vector3.left / 2 + Vector3.back / 32f;

				var vertIndex = i * vertArraySizeRatio;
				verts[vertIndex] = bottomLeft;
				verts[vertIndex + 1] = topLeft;
				verts[vertIndex + 2] = bottomRight;
				verts[vertIndex + 3] = topRight;

				var triIndex = i * triArraySizeRatio;
				triangles[triIndex] = vertIndex;        // bottomLeft
				triangles[triIndex + 1] = vertIndex + 1;    // topLeft
				triangles[triIndex + 2] = vertIndex + 2;    // bottomRight

				triangles[triIndex + 3] = vertIndex + 2;  // bottomRight
				triangles[triIndex + 4] = vertIndex + 1;  // topLeft
				triangles[triIndex + 5] = vertIndex + 3;  // topRight
			}

			meshRuler.vertices = verts;
			meshRuler.triangles = triangles;
			meshRuler.RecalculateNormals();
		}

		// Defines a series of line segments in gameboard space (-)
        public void ResetGrid(ScaleSettings newScaleSettings = null, GameBoardSettings newGameBoardSettings = null)
        {
			if (newScaleSettings != null)
			{
				this.scaleSettings = newScaleSettings;
			}
			if(newGameBoardSettings != null)
            {
				this.gameBoardSettings = newGameBoardSettings;
            }

            if(xGridLines == null)
			{
				xGridLines = new List<LineSegment>();
			}
			else xGridLines.Clear();

			if(zGridLines == null)
			{
				zGridLines = new List<LineSegment>();
			}
			else zGridLines.Clear();

			var lineLengthZ = 0.5f;
			var linePosOffsetZ = lineLengthZ - 0.5f;
			var lineStartPosZ = lineLengthZ;
			var lineStopPosZ = -lineLengthZ + linePosOffsetZ;
			var minDimension = Mathf.Min(usableGameBoardWidthInMeters, usableGameBoardLengthInMeters);
			var oneUnit = scaleSettings.oneUnitLengthInMeters / minDimension;
			// Starting from the center outward, define x-axis grid lines in 1-unit increments.
			for (int i = 0; i * scaleSettings.oneUnitLengthInMeters < usableGameBoardWidthInMeters / 2; i++)
            {
                float distanceFromCenter = i * (scaleSettings.oneUnitLengthInMeters / minDimension);
                int lod = 1;    // TODO: Change this later

                xGridLines.Add(new LineSegment(new Vector3(distanceFromCenter, 0, lineStartPosZ), new Vector3(distanceFromCenter, 0, lineStopPosZ), lod));

                // No need to draw a second overlapping pair of lines along the origin when i == 0.
                if(i < 1) {continue;}

                xGridLines.Add(new LineSegment(new Vector3(-distanceFromCenter, 0, lineStartPosZ), new Vector3(-distanceFromCenter, 0, lineStopPosZ), lod));
            }

			// Starting from the center outward, define z-axis grid lines in 1-unit increments.
			for (int i = 0; i * scaleSettings.oneUnitLengthInMeters < usableGameBoardWidthInMeters / 2; i++)
			{
				float distanceFromCenter = i * (scaleSettings.oneUnitLengthInMeters / minDimension);
				int lod = 1;	// TODO: Change this later

                zGridLines.Add(new LineSegment(new Vector3(0.5f, 0, distanceFromCenter), new Vector3(-0.5f, 0, distanceFromCenter), lod));

                // No need to draw a second overlapping pair of lines along the origin when i == 0.
                if(i < scaleSettings.oneUnitLengthInMeters) { continue; }

				zGridLines.Add(new LineSegment(new Vector3(0.5f, 0, -distanceFromCenter), new Vector3(-0.5f, 0, -distanceFromCenter), lod));
            }
        }


		public void Draw(ScaleSettings scaleSettings, GameBoardSettings gameBoardSettings,
			float alpha, bool showGrid, float gridOffsetY = 0f)
		{
			Configure (scaleSettings, gameBoardSettings, alpha, gridOffsetY, gameboardExtents);

			if (null == gameBoardSettings) { return; }


			Matrix4x4 mtxOrigin = Matrix4x4.TRS( Vector3.zero, Quaternion.identity, Vector3.one );

			Matrix4x4 mtxWorld = Matrix4x4.TRS( gameBoardSettings.gameBoardCenter,
				Quaternion.Euler(gameBoardSettings.gameBoardRotation),
				Vector3.one * scaleToUWRLD_UGBD);

			Matrix4x4 mtxPreTransform = Matrix4x4.TRS( Vector3.zero, Quaternion.Euler(0.0f, 0.0f, 0.0f), Vector3.one );

			Matrix4x4 mtxGizmo = mtxOrigin * mtxWorld * mtxPreTransform;

			Color oldColor = Gizmos.color;
			Matrix4x4 oldMatrix = Gizmos.matrix;

			Gizmos.matrix = mtxGizmo;

			Mesh meshBorder, meshSurface;

			switch (gameboardType)
            {
				case GameboardType.GameboardType_XE:
					meshBorder = meshBorder_XE;
					meshSurface = meshSurface_XE;
					break;
				case GameboardType.GameboardType_XE_Raised:
					meshBorder = meshBorder_XE_Raised;
					meshSurface = meshSurface_XE_Raised;
					break;
				default:
				case GameboardType.GameboardType_LE:
				case GameboardType.GameboardType_None:
					meshBorder = meshBorder_LE;
					meshSurface = meshSurface_LE;
					break;
            }

			if(meshBorder != null)
			{
				Gizmos.color = new Color(0.0f, 0.0f, 0.0f, alpha);
				Gizmos.DrawMesh(meshBorder, 0 );
			}

			if(meshSurface != null)
			{
				Gizmos.color = new Color (0.5f, 0.5f, 0.5f, alpha);
				Gizmos.DrawMesh(meshSurface, 0 );
			}

			if(meshLogoBorder != null && meshLogoLeftCharacter != null && meshLogoRightCharacter != null)
			{
				DrawLogo();
			}

			if(showGrid)
			{
				DrawGrid();
				DrawRulers();
			}

			Gizmos.matrix = oldMatrix;
			Gizmos.color = oldColor;
		}

		private void DrawLogo()
		{
			var logoDiameter = 2.5f;			// The logo mesh is about 12cm when imported. 2.5cm is better.
			var logoRadius = logoDiameter / 2f;	// 1.25cm diameter logo
			var borderThickness = new Length(GameBoard.GameboardExtents.BORDER_WIDTH_IN_METERS,  LengthUnit.Meters).ToCentimeters;
			var gameBoardFrontExtent = -gameBoardSettings.currentGameBoard.transform.forward / 2f;
			var gameBoardRightExtent = gameBoardSettings.currentGameBoard.transform.right / 2f;

			// Starting in the front-right corner...
			var logoPosition = (gameBoardRightExtent + gameBoardFrontExtent)  * usableGameBoardWidthInMeters;
			// ...move the logo left 5cm and center it on the game board border.
			var oneCentimeterLengthInMeters = new Length(1, LengthUnit.Centimeters).ToMeters;
			logoPosition -= gameBoardSettings.currentGameBoard.transform.right * 5f * oneCentimeterLengthInMeters;
			logoPosition -= gameBoardSettings.currentGameBoard.transform.forward * ((borderThickness / 2) - logoRadius) * oneCentimeterLengthInMeters;

			var contentScaleFactor = scaleToUWRLD_UGBD;
			Matrix4x4 mtxOrigin = Matrix4x4.TRS( Vector3.zero, Quaternion.identity, Vector3.one );
			var mtxWorld = Matrix4x4.TRS( gameBoardSettings.gameBoardCenter + logoPosition * contentScaleFactor,
				gameBoardSettings.currentGameBoard.rotation,
				logoDiameter * Vector3.one * contentScaleFactor );
			Matrix4x4 mtxPreTransform = Matrix4x4.TRS( Vector3.zero, Quaternion.Euler(-90.0f, 180.0f, 0.0f), Vector3.one );
			Gizmos.matrix = mtxOrigin * mtxWorld * mtxPreTransform;

			Gizmos.color = new Color (0.969f, 0.969f, 0.969f, gizmoAlpha);	// T5 Light Gray Background
			Gizmos.DrawMesh( meshLogoBorder, 0 );

			Gizmos.color = new Color (0.945f, 0.349f, 0.133f, gizmoAlpha);	// T5 Orange
			Gizmos.DrawMesh( meshLogoLeftCharacter, 0 );

			Gizmos.color = new Color (0.376f, 0.392f, 0.439f, gizmoAlpha);	// T5 Gray
			Gizmos.DrawMesh( meshLogoRightCharacter, 0 );
		}

        private void DrawGrid()
        {
			var contentScaleFactor = scaleToUWRLD_UGBD;
            float heightOffsetInMeters = yGridOffset * scaleSettings.oneUnitLengthInMeters;
			float lengthOffsetInMeters = Mathf.Max(usableGameBoardLengthInMeters - usableGameBoardWidthInMeters, 0f) / 2f;

			var gameBoardTransform = gameBoardSettings.currentGameBoard.transform;

			// Define the transformations for the grid origin and orientation.
			Matrix4x4 mtxOrigin = Matrix4x4.TRS( Vector3.zero, Quaternion.identity, Vector3.one );
			Matrix4x4 mtxPreTransform = Matrix4x4.TRS(
				Vector3.zero,
				Quaternion.Euler(0.0f, 0.0f, 0.0f),
				Vector3.one * Mathf.Min(usableGameBoardWidthInMeters, usableGameBoardLengthInMeters));
				//new Vector3(usableGameBoardWidthInMeters, 1f, usableGameBoardLengthInMeters));
            Matrix4x4 mtxWorld = Matrix4x4.TRS(
				gameBoardSettings.gameBoardCenter + (gameBoardTransform.up * heightOffsetInMeters / contentScaleFactor),
				gameBoardTransform.rotation,
				Vector3.one / contentScaleFactor );
			Matrix4x4 mtxGizmo = mtxOrigin * mtxWorld * mtxPreTransform;
			Gizmos.matrix = mtxGizmo;

			// Unless we're using inches, grid lines should appear in groups of 10.
			var gridLinePeriod = scaleSettings.contentScaleUnit == LengthUnit.Inches ? 12 : 10;

			// We want to fade out grid lines spaced closer than 10 pixels on the screen.
			// We'll do two iterations of fading out.
			float beginFadeThreshold = 20f;
			float endFadeThreshold = 10f;

			// To determine the spacing, we'll sample a position under the camera (even if it's off the board).
			TryGetCameraPositionOverGameBoard(out var cameraPositionOverGameBoard);
			Vector3 testUnitVector = cameraPositionOverGameBoard + Vector3.right * scaleSettings.oneUnitLengthInMeters / contentScaleFactor;
			Vector3 secondTestUnitVector = cameraPositionOverGameBoard + Vector3.right * gridLinePeriod * scaleSettings.oneUnitLengthInMeters / contentScaleFactor;

			var screenSpaceDistance = (SceneView.currentDrawingSceneView.camera.WorldToScreenPoint(testUnitVector)
				- SceneView.currentDrawingSceneView.camera.WorldToScreenPoint(cameraPositionOverGameBoard)).magnitude;
			var secondScreenSpaceDistance = (SceneView.currentDrawingSceneView.camera.WorldToScreenPoint(secondTestUnitVector)
				- SceneView.currentDrawingSceneView.camera.WorldToScreenPoint(cameraPositionOverGameBoard)).magnitude;

			// Check the sampled values against the thresholds we defined above and calculate transparency values.
			var beginFade = screenSpaceDistance < beginFadeThreshold;
			var endFade = screenSpaceDistance < endFadeThreshold;

			var beginSecondFade = secondScreenSpaceDistance < beginFadeThreshold;
			var endSecondFade = secondScreenSpaceDistance < endFadeThreshold;

			var fadeFactor = 1f - Mathf.Clamp((beginFadeThreshold - screenSpaceDistance) / endFadeThreshold, 0f, 1f);
			var secondFadeFactor = 1f - Mathf.Clamp((beginFadeThreshold - secondScreenSpaceDistance) / endFadeThreshold, 0f, 1f);

			var fadeAlpha = gizmoAlpha * fadeFactor;
			var secondFadeAlpha = gizmoAlpha * secondFadeFactor;

			// Finally, draw some grid lines.
			var oldColor = Gizmos.color;

			DrawGridLines(xGridLines, gridLinePeriod, beginFade, endFade, fadeAlpha, beginSecondFade, endSecondFade, secondFadeAlpha);
			DrawGridLines(zGridLines, gridLinePeriod, beginFade, endFade, fadeAlpha, beginSecondFade, endSecondFade, secondFadeAlpha);

			Gizmos.color = oldColor;
        }

		private void DrawGridLines(List<LineSegment> lines, int gridLinePeriod, bool beginFade, bool endFade,
			float fadeAlpha, bool beginSecondFade, bool endSecondFade, float secondFadeAlpha)
		{
			var gridLinePeriodSquared = gridLinePeriod * gridLinePeriod;
			var mtxGizmo = Gizmos.matrix;

			// We want to use a sphere around the projected cursor position to check for collision with lines on the grid.
			// Any lines that collide will be colored differently. Set the sphere diameter to a single content unit, and scale it as the grid fades.
			float cursorSphereRadius = scaleToUWRLD_UGBD * (scaleSettings.oneUnitLengthInMeters / 2f);
			if(endSecondFade)
			{
				cursorSphereRadius *= (gridLinePeriod * gridLinePeriod) / 2f;
			}
			else if(endFade)
			{
				cursorSphereRadius *= gridLinePeriod / 2f;
			}

			for(int i = 0; i < lines.Count; i++)
            {
				// Determine whether we should skip drawing this line.
				var isPeriodic = i % gridLinePeriod == 0 || i % gridLinePeriod == gridLinePeriod - 1;
				if(endFade && !isPeriodic) { continue; }

				var isDoublePeriodic = i % gridLinePeriodSquared == 0 || i % gridLinePeriodSquared == gridLinePeriodSquared - 1;
				if(endSecondFade && !isDoublePeriodic) { continue; }

				// We'll need the current line defined in world space to check for cursor collision.
                var currentGridLineSegment = lines[i];
                var transformedLineSegment = new LineSegment(
					mtxGizmo.MultiplyPoint(currentGridLineSegment.Start),
					mtxGizmo.MultiplyPoint(currentGridLineSegment.End), currentGridLineSegment.LOD);

				var lineCollidesWithCursor = TryGetCursorPosition(out var cursorPosition)
					&& CheckRaySphereCollision(cursorPosition, cursorSphereRadius, transformedLineSegment);

				// Apply any fading required by the camera's distance from the grid.
				var lineAlpha = gizmoAlpha;
				if(beginSecondFade && !isDoublePeriodic)
				{
					lineAlpha = secondFadeAlpha;
				}
				else if(beginFade && !isPeriodic)
				{
					lineAlpha = fadeAlpha;
				}

				Gizmos.color = lineCollidesWithCursor
					? new Color(1f, 0f, 0f, gizmoAlpha)
					: new Color(1f, 1f, 1f, lineAlpha);

				// If the line collides with the cursor, project it on the edges of the board.
				if(lineCollidesWithCursor)
				{
					var oldGizmoMatrix = Gizmos.matrix;
					var xLineExtensionFactor = totalGameBoardWidthInMeters / usableGameBoardWidthInMeters;
					var zLineExtensionFactor = totalGameBoardLengthInMeters / usableGameBoardLengthInMeters;

					// We need to know which direction to extend it. It's either x-axis aligned or z-axis aligned.
					var lineDifference = currentGridLineSegment.End - currentGridLineSegment.Start;
					var xAxisAligned = Mathf.Abs(Vector3.Dot(Vector3.right, lineDifference)) == 1;

					var mtxPreTransform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
						(xAxisAligned ? new Vector3(xLineExtensionFactor, 1f, 1f) : new Vector3(1f, 1f, zLineExtensionFactor)));
					var newMtxWorld = Matrix4x4.TRS(
						gameBoardSettings.currentGameBoard.transform.position,
						gameBoardSettings.currentGameBoard.transform.rotation,
						new Vector3(scaleToUWRLD_UGBD * usableGameBoardWidthInMeters, 0f, scaleToUWRLD_UGBD * usableGameBoardWidthInMeters));
					Gizmos.matrix = newMtxWorld;

					// Finally, draw a line.
					Gizmos.DrawLine(currentGridLineSegment.Start, mtxPreTransform.MultiplyPoint(currentGridLineSegment.Start));
					Gizmos.DrawLine(currentGridLineSegment.End, mtxPreTransform.MultiplyPoint(currentGridLineSegment.End));
					Gizmos.matrix = oldGizmoMatrix;
				}
				Gizmos.DrawLine(currentGridLineSegment.Start, currentGridLineSegment.End);
            }
		}


		private bool TryGetCursorPosition(out Vector3 intersectPoint)
        {
			var sceneView = UnityEditor.SceneView.currentDrawingSceneView;
			var sceneViewCamera = sceneView.camera;
			var mousePos = Event.current.mousePosition - sceneView.position.position;

            // Get the mouse position
            mousePos = GUIUtility.GUIToScreenPoint(mousePos);

			// The calls above tends to include some extra invisible space above the rendered scene view, so we compensate.
            var frameOffset = Vector2.down * 36f;
            mousePos.y = (sceneViewCamera.pixelHeight - mousePos.y - frameOffset.y);

            var ray = sceneViewCamera.ScreenPointToRay(mousePos);

			return TryGetGridPlanePosition(ray, out intersectPoint);
        }

		private bool TryGetCameraPositionOverGameBoard(out Vector3 intersectPoint)
		{
			var ray = new Ray(UnityEditor.SceneView.currentDrawingSceneView.camera.transform.position, -gameBoardSettings.currentGameBoard.transform.up);
			return TryGetGridPlanePosition(ray, out intersectPoint);
		}

		private bool TryGetGridPlanePosition(Ray ray, out Vector3 intersectPoint)
		{
			// Set up the collision plane
            var planeOffsetY = (yGridOffset * scaleSettings.oneUnitLengthInMeters) / scaleToUWRLD_UGBD;
            var gridPlane = new Plane(
				gameBoardSettings.currentGameBoard.transform.up,
				gameBoardSettings.currentGameBoard.transform.localToWorldMatrix.MultiplyPoint(planeOffsetY * Vector3.up));

            if(gridPlane.Raycast(ray, out var intersectionDistance))
            {
                // Get the point along the ray intersecting the plane.
                intersectPoint = ray.GetPoint(intersectionDistance);
                return true;
            }

            intersectPoint = Vector3.zero;
            return false;
		}

		private bool CheckRaySphereCollision(Vector3 sphereCenter, float sphereRadius, LineSegment lineSegment){

			var ray = new Ray(lineSegment.Start, lineSegment.End - lineSegment.Start);

            Vector3 rayToSphereCenterDistance = ray.origin - sphereCenter;
            float a = Vector3.Dot(ray.direction, ray.direction);
            float b = 2f * Vector3.Dot(rayToSphereCenterDistance, ray.direction);
            float c = Vector3.Dot(rayToSphereCenterDistance, rayToSphereCenterDistance) - sphereRadius * sphereRadius;
            float discriminant = (b * b) - (4*a*c);
            return discriminant > 0;
        }

		private void DrawRulers()
		{
			if(meshRuler == null) { return; }

			Gizmos.color = new Color (1f, 0.8320962f, 0.3803922f, gizmoAlpha);

			var contentScaleFactor = scaleToUWRLD_UGBD;
			float borderWidthInMeters = GameBoard.GameboardExtents.BORDER_WIDTH_IN_METERS;
			float lengthOffsetInMeters = Mathf.Max(usableGameBoardLengthInMeters - usableGameBoardWidthInMeters, 0f) / 2f;
			var gameBoardTransform = gameBoardSettings.currentGameBoard.transform;
			var gameboardCenter = gameBoardSettings.gameBoardCenter;

			Matrix4x4 mtxOrigin = Matrix4x4.TRS( Vector3.zero, Quaternion.identity, Vector3.one );
			Matrix4x4 mtxPreTransform = Matrix4x4.TRS( Vector3.zero, Quaternion.Euler(0.0f, 0.0f, 0.0f), new Vector3(usableGameBoardWidthInMeters, 1f, usableGameBoardLengthInMeters));

			// Far edge
			Matrix4x4 mtxWorld = Matrix4x4.TRS(gameboardCenter + (gameBoardTransform.forward * lengthOffsetInMeters / contentScaleFactor) + (0.5f * (usableGameBoardLengthInMeters + borderWidthInMeters) * gameBoardTransform.forward) / contentScaleFactor,
				Quaternion.Euler(gameBoardSettings.gameBoardRotation),
				Vector3.one / contentScaleFactor );
			Gizmos.matrix = mtxOrigin * mtxWorld * mtxPreTransform;
			Gizmos.DrawMesh(meshRuler, 0 );

			// Near edge
			mtxWorld = Matrix4x4.TRS(gameboardCenter - (0.5f * (usableGameBoardWidthInMeters + borderWidthInMeters) * gameBoardTransform.forward) / contentScaleFactor,
				Quaternion.Euler(gameBoardSettings.gameBoardRotation),
				Vector3.one / contentScaleFactor );
			Gizmos.matrix = mtxOrigin * mtxWorld * mtxPreTransform;
			Gizmos.DrawMesh(meshRuler, 0 );

			// Right edge
			mtxPreTransform = Matrix4x4.TRS( Vector3.zero, Quaternion.Euler(0.0f, 90.0f, 0.0f), Vector3.one * usableGameBoardWidthInMeters);
			mtxWorld = Matrix4x4.TRS(gameboardCenter + (0.5f * (usableGameBoardWidthInMeters + borderWidthInMeters) * gameBoardTransform.right) / contentScaleFactor,
				Quaternion.Euler(gameBoardSettings.gameBoardRotation),
				Vector3.one / contentScaleFactor );
			Gizmos.matrix = mtxOrigin * mtxWorld * mtxPreTransform;
			Gizmos.DrawMesh(meshRuler, 0 );

			// Left Edge
			mtxWorld = Matrix4x4.TRS(gameboardCenter - (0.5f * (usableGameBoardWidthInMeters + borderWidthInMeters) * gameBoardTransform.right) / contentScaleFactor,
				Quaternion.Euler(gameBoardSettings.gameBoardRotation),
				Vector3.one / contentScaleFactor );
			Gizmos.matrix = mtxOrigin * mtxWorld * mtxPreTransform;
			Gizmos.DrawMesh(meshRuler, 0 );
		}
	}
}
#endif
