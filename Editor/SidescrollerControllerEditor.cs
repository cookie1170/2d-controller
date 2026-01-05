using UnityEditor;
using UnityEngine;

namespace Cookie.PlayerController.Editor
{
    [CustomEditor(typeof(SidescrollerController))]
    public class SidescrollerControllerEditor : UnityEditor.Editor
    {
        private Collider2D _collider;

        private void OnSceneGUI()
        {
            var controller = target as SidescrollerController;
            if (!_collider)
                _collider = controller.GetComponent<Collider2D>();

            float size = HandleUtility.GetHandleSize(controller.transform.position) * 0.1f;
            float snap = 0.05f;
            float height = controller.jumpHeight;
            Vector3 horizontalOffset = Vector3.right * 0.25f;

            EditorGUI.BeginChangeCheck();

            Vector3 handlePosition = controller.transform.position + (Vector3.up * height);
            Handles.DrawDottedLine(controller.transform.position, handlePosition, 10f);

            if (_collider)
            {
                Vector3 verticalOffset = Vector3.up * _collider.bounds.extents.y;
                Vector3 topPosition = handlePosition + verticalOffset;
                Vector3 bottomPosition = handlePosition - verticalOffset;

                Handles.DrawLine(topPosition - horizontalOffset, topPosition + horizontalOffset);
                Handles.DrawLine(
                    bottomPosition - horizontalOffset,
                    bottomPosition + horizontalOffset
                );
            }

            height = (
                Handles.Slider(handlePosition, Vector3.up, size, Handles.CubeHandleCap, snap)
                - controller.transform.position
            ).y;

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(
                    controller,
                    $"Changed {controller.name}'s jump height to {height}"
                );
                controller.jumpHeight = height;
            }
        }
    }
}
