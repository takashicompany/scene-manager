namespace takashicompany.Unity.SceneManagement
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using UnityEngine;

	/// <summary>
	/// 簡易シーンマネージャー
	/// 同じシーンを複数ロードできる
	/// </summary>
	public class SceneManager
	{
		public delegate void LoadDelegate(Scene scene);
		public delegate void LoadDelegate<T>(T result) where T : Scene;
		public delegate void DestoryDelegate();

		private HashSet<Scene> _loadedScenes = new HashSet<Scene>();

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
						TryInitScene(scene, sceneFile);
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

		public IEnumerable<Scene> GetLoadedScenes()
		{
			return _loadedScenes;
		}

		public T Get<T>() where T: Scene
		{
			var sceneType = typeof(T);
			var scene = _loadedScenes.FirstOrDefault(m => m.GetType() == sceneType);
			return (T)scene;
		}

		public List<Scene> GetAll<T>() where T: Scene
		{
			var sceneType = typeof(T);
			return _loadedScenes.Where(m => m.GetType() == sceneType).ToList();
		}

		public void FindOrLoadAsync<T>(LoadDelegate<T> callback) where T : Scene
		{
			var scene = Get<T>();

			if (scene != null)
			{
				if (callback != null)
				{
					callback((T)scene);
				}
			}
			else
			{
				var s = FindLoadedScene(typeof(T));

				if (s != null)
				{
					callback((T)s);
				}
				else
				{
					LoadAsync<T>(callback);
				}
			}
		}

		public void LoadAsync<T>(LoadDelegate<T> callback) where T : Scene
		{
			var type = typeof(T);
			LoadAsync(type, scene =>
			{
				if (callback != null)
				{
					callback((T)scene);
				}
			});
		}

		public void LoadAsync(Type type, LoadDelegate callback)
		{
			var sceneName = type.Name;
			var asyncOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
			
			System.Action<AsyncOperation> task = async =>
			{
				var scene = FindLoadedScene(type);

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

		private Scene FindLoadedScene(Type type)
		{
			var sceneName = type.Name;
			var sceneFile = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
			
			if (!sceneFile.IsValid())
			{
				return null;
			}

			Scene result = null;

			foreach (var root in sceneFile.GetRootGameObjects())
			{
				var scene = root.GetComponent<Scene>();
				if (scene != null)
				{
					TryInitScene(scene, sceneFile);
					if (result == null)
					{
						result = scene;
					}
				}
			}
			
			return result;
		}

		private void TryInitScene(Scene scene, UnityEngine.SceneManagement.Scene sceneFile)
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

			_loadedScenes.RemoveWhere(m => m.GetType() == sceneType);

			var sceneName = sceneType.Name;
			UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneName);
		}

		public void Unload<T>(DestoryDelegate callback) where T : Scene
		{
			var sceneType = typeof(T);
			_loadedScenes.RemoveWhere(m => m.GetType() == sceneType);

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
			_loadedScenes.Remove(scene);
			var sceneFile = scene.sceneFile;
			UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneFile);
		}

		public void Unload(Scene scene, DestoryDelegate callback)
		{
			_loadedScenes.Remove(scene);

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
