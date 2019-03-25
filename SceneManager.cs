namespace TakashiCompany.Unity
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

	/// <summary>
	/// 簡易シーンマネージャー
	/// 同じシーンを複数ロードできる
	/// </summary>
	public class SceneManager : MonoBehaviour
	{
		public delegate void LoadDelegate<T>(T result) where T : Scene;
		public delegate void DestoryDelegate();

		private List<Scene> _loadedScenes = new List<Scene>();

		public delegate void InitDelegate(Scene scene);
		
		public event InitDelegate onInitEvent;

		public event InitDelegate onInitCompleteEvent;

		public void Init()
		{
			var count = UnityEngine.SceneManagement.SceneManager.sceneCount;

			for (int i = 0; i < count; i++)
			{
				var sceneFile = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
				foreach (var root in sceneFile.GetRootGameObjects())
				{
					var scene = root.GetComponent<Scene>();
					if (scene != null)
					{
						InitScene(scene, sceneFile);
					}
				}
			}
		}

		public T Find<T>() where T: Scene
		{
			var sceneType = typeof(T);
			var scene = _loadedScenes.Find(m => m.GetType() == sceneType);
			return (T)scene;
		}

		public List<Scene> FindAll<T>() where T: Scene
		{
			var sceneType = typeof(T);
			return _loadedScenes.FindAll(m => m.GetType() == sceneType);
		}

		public void FindOrLoadAsync<T>(LoadDelegate<T> callback) where T : Scene
		{
			var scene = Find<T>();

			if (scene != null)
			{
				if (callback != null)
				{
					callback((T)scene);
				}
			}
			else
			{
				scene = ProcessSceneFile<T>();

				if (scene != null)
				{
					callback((T)scene);
				}

				LoadAsync<T>(callback);
			}
		}

		public void LoadAsync<T>(LoadDelegate<T> callback) where T : Scene
		{
			StartCoroutine(CoLoadAsync<T>(callback));
		}

		public IEnumerator CoLoadAsync<T>(LoadDelegate<T> callback) where T : Scene
		{
			var sceneName = typeof(T).Name;

			var asyncOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
			
			while (!asyncOperation.isDone)
			{
				yield return null;
			}

			var resultScene = ProcessSceneFile<T>();

			if (callback != null)
			{
				callback(resultScene);
			}
		}

		private T ProcessSceneFile<T>() where T : Scene
		{
			var sceneName = typeof(T).Name;
			var sceneFile = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
			
			if (!sceneFile.IsValid())
			{
				return null;
			}

			T resultScene = null;

			foreach (var root in sceneFile.GetRootGameObjects())
			{
				var scene = root.GetComponent<Scene>();
				if (scene != null)
				{
					InitScene(scene, sceneFile);
					if (resultScene == null && typeof(T) == scene.GetType())
					{
						resultScene = (T)scene;
					}
				}
			}
			
			return resultScene;
		}

		private void InitScene(Scene scene, UnityEngine.SceneManagement.Scene sceneFile)
		{
			if (!_loadedScenes.Contains(scene))
			{
				_loadedScenes.Add(scene);
				
				scene.Init(this, sceneFile);

				if (onInitEvent != null)
				{
					onInitEvent(scene);
				}
				
				scene.OnInit();

				if (onInitCompleteEvent != null)
				{
					onInitCompleteEvent(scene);
				}
			}
		}

		public void Unload<T>() where T : Scene
		{
			var sceneType = typeof(T);
			_loadedScenes.RemoveAll(m => m.GetType() == sceneType);

			var sceneName = sceneType.Name;
			UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneName);
		}

		public void Unload<T>(DestoryDelegate callback) where T : Scene
		{
			StartCoroutine(CoUnload<T>(callback));
		}

		private IEnumerator CoUnload<T>(DestoryDelegate callback) where T : Scene
		{
			var sceneType = typeof(T);
			_loadedScenes.RemoveAll(m => m.GetType() == sceneType);

			var sceneName = sceneType.Name;
			var asyncOperation = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneName);

			while (!asyncOperation.isDone)
			{
				yield return null;
			}

			if (callback != null)
			{
				callback();
			}
		}

		public void Unload(Scene scene)
		{
			_loadedScenes.RemoveAll(m => m.sceneFile == scene.sceneFile);
			var sceneFile = scene.sceneFile;
			UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneFile);
		}

		public void Unload(Scene scene, DestoryDelegate callback)
		{
			StartCoroutine(CoUnload(scene, callback));
		}

		private IEnumerator CoUnload(Scene scene, DestoryDelegate callback)
		{
			_loadedScenes.RemoveAll(m => m.sceneFile == scene.sceneFile);

			var asyncOperation = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene.sceneFile);

			while (!asyncOperation.isDone)
			{
				yield return null;
			}

			if (callback != null)
			{
				callback();
			}
		}
	}
}