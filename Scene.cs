namespace takashicompany.Unity.SceneManagement
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

	/// <summary>
	/// この抽象クラスを継承したクラスを、シーンファイル内のRootのGameObjectにアタッチしてください。
	/// シーンファイル内にSceneクラスは一つとする。
	/// </summary>
	public abstract class Scene : MonoBehaviour
	{
		public UnityEngine.SceneManagement.Scene sceneFile { get; private set; }

		protected SceneManager _sceneManager;

		public void Init(SceneManager sceneManager,UnityEngine.SceneManagement.Scene sceneFile)
		{
			_sceneManager = sceneManager;
			this.sceneFile = sceneFile;
		}

		protected virtual void Start()
		{
			if (_sceneManager == null)
			{
				var sceneManager = new SceneManager();
				sceneManager.Init();
			}
		}

		/// <summary>
		/// SceneManagerに登録された際に一度だけ呼ばれる
		/// </summary>
		public virtual void OnInit()
		{
			
		}

		/// <summary>
		/// このシーンから再生を開始したら呼ばれる
		/// </summary>
		public virtual void OnLaunch()
		{

		}
	}
}
