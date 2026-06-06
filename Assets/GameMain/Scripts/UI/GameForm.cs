using GameFramework.Event;
using UnityEngine;
using UnityEngine.UI;

namespace StarForce
{
    public class GameForm : UGuiForm
    {
        [SerializeField]
        private Text m_ScoreText = null;

        private int m_Score = 0;

        public void UpdateScoreText()
        {
            m_ScoreText.text = GameFramework.Utility.Text.Format("{0}", m_Score);
        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            GameEntry.Event.Subscribe(AsteroidDeadEventArgs.EventId, OnAsteroidDead);
            UpdateScoreText();
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            base.OnClose(isShutdown, userData);

            GameEntry.Event.Unsubscribe(AsteroidDeadEventArgs.EventId, OnAsteroidDead);
        }

        private void OnAsteroidDead(object sender, GameEventArgs e)
        {
            AsteroidDeadEventArgs ne = (AsteroidDeadEventArgs)e;
            m_Score += ne.Score;
            UpdateScoreText();
        }
    }
}