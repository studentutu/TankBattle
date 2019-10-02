using System;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;

namespace UnityEditor.VisualScripting.Editor
{
    static class PropertiesReducers
    {
        public static void Register(Store store)
        {
            store.Register<CreateGetPropertyGroupNodeAction>(CreateGetPropertyGroupNode);
            store.Register<CreateSetPropertyGroupNodeAction>(CreateSetPropertyGroupNode);
            store.Register<EditPropertyGroupNodeAction>(EditPropertyGroupNode);
        }

        static State CreateGetPropertyGroupNode(State previousState, CreateGetPropertyGroupNodeAction action)
        {
            ((VSGraphModel)previousState.CurrentGraphModel).CreateGetPropertyGroupNode(action.Position);
            previousState.MarkForUpdate(UpdateFlags.GraphTopology);
            return previousState;
        }

        static State CreateSetPropertyGroupNode(State previousState, CreateSetPropertyGroupNodeAction action)
        {
            ((StackBaseModel)action.StackModel).CreateSetPropertyGroupNode(action.Index);
            previousState.MarkForUpdate(UpdateFlags.GraphTopology);
            return previousState;
        }

        static State EditPropertyGroupNode(State previousState, EditPropertyGroupNodeAction action)
        {
            var propertyGroupBase = action.nodeModel as PropertyGroupBaseNodeModel;
            if (propertyGroupBase == null)
                return previousState;

            Undo.RegisterCompleteObjectUndo(propertyGroupBase.NodeAssetReference, "Remove Members");
            switch (action.editType)
            {
                case EditPropertyGroupNodeAction.EditType.Add:
                    propertyGroupBase.AddMember(action.member);
                    EditorUtility.SetDirty(propertyGroupBase.NodeAssetReference);
                    break;

                case EditPropertyGroupNodeAction.EditType.Remove:
                    propertyGroupBase.RemoveMember(action.member);
                    EditorUtility.SetDirty(propertyGroupBase.NodeAssetReference);
                    break;
            }

            return previousState;
        }
    }
}
