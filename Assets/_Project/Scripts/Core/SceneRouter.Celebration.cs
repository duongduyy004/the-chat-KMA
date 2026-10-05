using UnityEngine;

namespace KMA.Gameplay.Core
{
    public sealed partial class SceneRouter
    {
        [SerializeField] string celebrationScene = "Celebration";

        public string CelebrationScene => celebrationScene;

        /// The course-complete celebration. Allowed only once the course is complete.
        public bool RouteToCelebration()
        {
            lastRouteError = null;
            if (IsTransitioning || session.ActiveSubject.HasValue || !session.Journey.CourseComplete)
                return false;
            return Route(SessionRoute.Celebration);
        }
    }
}
