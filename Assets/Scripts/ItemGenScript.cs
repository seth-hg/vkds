using UnityEngine;
using System.Collections;

public class ItemGenScript : MonoBehaviour {
	private bool[,] locationMap; 
	private int Cols, Rows;
	private int bgWidth, bgHeight;			// 背景尺寸
	private int gridWidth, gridHeight;		// 网格尺寸
	
	public void initMap(int nCols, int nRows, int width, int height) {
		int col, row;
		locationMap = new bool[nCols, nRows];
		for (col = 0; col < nCols; col ++)
			for (row = 0; row < nRows; row ++)
				locationMap [col, row] = false;
		Cols = nCols;
		Rows = nRows;
		bgWidth = width;
		bgHeight = height;
		
		// 初始化道具网格
		gridWidth = bgWidth / 10;
		gridHeight = bgHeight / 20;
	}
	
	public Vector2 allocItem (bool reversed) {
		int iCol, iRow;
		
		while (true) {
			iCol = Random.Range (0, Cols);
			iRow = Random.Range (0, Rows);

			if (reversed) {
				iCol = Cols - 1 - iCol;
				iRow = Rows - 1 - iRow;
			}

			if (available(iCol, iRow)) {
				// if this position is not occupied
				break;
			}
		}
		markLocation (iCol, iRow);
		
		return grid2pos(iCol, iRow);
	}
	
	bool available(int col, int row) {
		return locationMap [col, row] == false;
	}
	
	void markLocation (int col, int row) {
		locationMap [col, row] = true;
	}
	
	void clearLocation (int col, int row) {
		locationMap [col, row] = false;
	}
	
	public Vector2 grid2pos(int col, int row) {
		float x = ((col + 1.0f) * gridWidth - bgWidth / 2) / 100.0f;
		float y = ((row + 1.0f) * gridHeight - bgHeight / 2)/ 100.0f;
		
		return new Vector2 (x, y);
	}
	
	public Vector2 pos2grid(float x, float y) {
		float col, row;
		
		x = x - gridWidth / 2;
		y = y - gridHeight / 2;
		if (x < 0)
			col = 0;
		else
			col = x / gridWidth;
		
		if (y < 0)
			row = 0;
		else
			row = y / gridHeight;
		
		return new Vector2 (col, row);
	}
	
	public void markPosition (Vector2 pos) {
		Vector2 loc = pos2grid (pos.x, pos.y);
		markLocation ((int)loc.x, (int)loc.y);
	}
	
	public void clearPosition (Vector2 pos) {
		Vector2 loc = pos2grid (pos.x, pos.y);
		clearLocation ((int)loc.x, (int)loc.y);
	}
	// Use this for initialization
	void Start () {
		initMap (9, 19, bgWidth, bgHeight);
		
		// 标记Actor所在的位置
		GameObject[] actors = GameObject.FindGameObjectsWithTag ("player");
		foreach (GameObject a in actors) {
			markPosition(a.transform.position);
		}
		
		actors = GameObject.FindGameObjectsWithTag ("enemy");
		foreach (GameObject a in actors) {
			markPosition(a.transform.position);
		}
	}
}
