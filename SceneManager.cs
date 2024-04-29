namespace takashicompany.Unity.SceneManagement
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

	/// <summary>
	/// 簡易シーンマネージャー
	/// 同じシーンを複数ロードできる
	/// </summary>
	public class SceneManager
	{
		public delegate void LoadDelegate<T>(T result) where T : Scene;
		public delegate void DestoryDelegate();

		private List<Scene> _loadedScenes = new List<Scene>();

		public delegate void InitDelegate(Scene scene);
		
		public event InitDelegate onInitEvent;

		public event InitDelegate onInitCompleteEvent;

		public SceneManager()
		{
			
		}

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

			var mainSceneFile = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

			foreach (var scene in _loadedScenes)
			{
				if (mainSceneFile == scene.sceneFile)
				{
					scene.OnLaunch();
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
			var sceneName = typeof(T).Name;
			var asyncOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
			
			System.Action<AsyncOperation> task = async =>
			{
				var scene = ProcessSceneFile<T>();

				if (callback != null)
				{
					callback(scene);
				}
			};

			if (asyncOperation.isDone)
			{
				task(asyncOperation);
			}
			else
			{
				asyncOperation.completed += task;
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

			T result = null;

			foreach (var root in sceneFile.GetRootGameObjects())
			{
				var scene = root.GetComponent<Scene>();
				if (scene != null)
				{
					InitScene(scene, sceneFile);
					if (result == null && typeof(T) == scene.GetType())
					{
						result = (T)scene;
					}
				}
			}
			
			return result;
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
			var sceneType = typeof(T);
			_loadedScenes.RemoveAll(m => m.GetType() == sceneType);

			var sceneName = sceneType.Name;
			var asyncOperation = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneName);

			System.Action<AsyncOperation> task = async =>
			{
				if (callback != null)
				{
					callback();
				}
			};

			if (asyncOperation.isDone)
			{
				task(asyncOperation);
			}
			else
			{
				asyncOperation.completed += task;
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
			_loadedScenes.RemoveAll(m => m.sceneFile == scene.sceneFile);

			var asyncOperation = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene.sceneFile);

			System.Action<AsyncOperation> task = async =>
			{
				if (callback != null)
				{
					callback();
				}
			};

			if (asyncOperation.isDone)
			{
				task(asyncOperation);
			}
			else
			{
				asyncOperation.completed += task;
			}
		}
	}
}
