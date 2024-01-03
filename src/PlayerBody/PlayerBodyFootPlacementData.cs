using UnityEngine;
using System.Collections;

namespace PlayerBodySystem
{
    public class PlayerBodyFootPlacementData : MonoBehaviour
    {
        [Header("This component contains data used to modify foot placement.")]
        [Header("There should be one for each foot on this game object.")]
        [Header("You can try changing settings if your feet aren't being correctly planted on the ground.")]
        private Animator _playerBodyAnimator;

        public enum LimbID
        {
            LEFT_FOOT = 0,
            RIGHT_FOOT = 1,
            LEFT_HAND = 2,
            RIGHT_HAND = 3,
        }

        public enum Target
        {
            FOOT = 0,
            TOE = 1,
            HEEL = 2
        }

        public LimbID mFootID = LimbID.LEFT_FOOT;
        public bool mPlantFoot = true;

        public Vector3 mForwardVector = new Vector3(0, 0, 1);
        public Vector3 mIKHintOffset = new Vector3(0, 0, 0);
        public Vector3 mUpVector = new Vector3(0, 1, 0);

        public float mFootOffsetDist = 0.5f;
        public float mFootLength = 0.22f;
        public float mFootHalfWidth = 0.05f;
        public float mFootHeight = 0.1f;
        public float mFootRotationLimit = 45;
        public float mTransitionTime = 0.2f;
        public float mExtraRayDistanceCheck = 0.0f;

        //foot stability parameteres
        public bool mSetExtraRaydistanceCheckAutomatically = false;
        public float mErrorThreshold = 0.05f;
        public float mExtraRayDistanceCheckMin = 0;
        public float mExtraRayDistanceCheckMax = 2;



        protected bool mFootPlantIsOnTransition = false;
        protected float mFootPlantBlendSpeed;

        protected Vector3 mTargetPos = new Vector3(0, 0, 0);
        protected Vector3 mTargetToePos = new Vector3(0, 0, 0);
        protected Vector3 mTargetHeelPos = new Vector3(0, 0, 0);

        protected Vector3 mRotatedFwdVec;
        protected Vector3 mRotatedIKHintOffset;

        protected float mTargetFootWeight = 0;
        protected float mCurrentFootWeight = 0;
        protected float mGoalBlendSpeed = 0;
        protected float mPlantBlendFactor = 0;

        private Vector3 mFootPlantedPos;
        private Quaternion mFootPlantedRot;
        private bool mFootPlanted = false;

        //foot stability parameteres
        private Vector3 mPreviousFootPos = Vector3.zero;

        /*****************************************/
        public void SetTargetPos(Target target, Vector3 target_pos)
        {
            switch (target)
            {
                case Target.FOOT:
                    mTargetPos = target_pos;
                    break;

                case Target.TOE:
                    mTargetToePos = target_pos;
                    break;

                case Target.HEEL:
                    mTargetHeelPos = target_pos;
                    break;
            }
        }

        /*****************************************************/
        public Vector3 GetTargetPos(Target target)
        {
            switch (target)
            {
                case Target.FOOT:
                    return mTargetPos;

                case Target.TOE:
                    return mTargetToePos;

                case Target.HEEL:
                    return mTargetHeelPos;
            }

            return Vector3.zero;
        }

        /**********************************/
        public void CalculateRotatedFwdVec()
        {
            AvatarIKGoal lFootID = AvatarIKGoal.LeftFoot;

            switch (mFootID)
            {
                case LimbID.RIGHT_FOOT:
                    lFootID = AvatarIKGoal.RightFoot;
                    break;
                case LimbID.LEFT_HAND:
                    lFootID = AvatarIKGoal.LeftHand;
                    break;
                case LimbID.RIGHT_HAND:
                    lFootID = AvatarIKGoal.RightHand;
                    break;
            }

            float lAngle = 0;
            Quaternion lYawRotation;

            lAngle = _playerBodyAnimator.GetIKRotation(lFootID).eulerAngles.y * Mathf.PI / 180;
            lYawRotation = new Quaternion(0, Mathf.Sin(lAngle * 0.5f), 0, Mathf.Cos(lAngle * 0.5f));


            if (mFootPlanted && mPlantFoot)
            {
                lAngle = mFootPlantedRot.eulerAngles.y * Mathf.PI / 180;
                lYawRotation = Quaternion.Slerp(lYawRotation, new Quaternion(0, Mathf.Sin(lAngle * 0.5f), 0, Mathf.Cos(lAngle * 0.5f)), mPlantBlendFactor);
            }
            mRotatedFwdVec = lYawRotation * mForwardVector.normalized;

        }

        /*******************************/
        public Vector3 GetRotatedFwdVec()
        {
            return mRotatedFwdVec;
        }

        /*******************************************/
        public void CalculateRotatedIKHint()
        {
            float lAngle = transform.rotation.eulerAngles.y * Mathf.PI / 180;
            Quaternion lYawRotation = new Quaternion(0, Mathf.Sin(lAngle * 0.5f), 0, Mathf.Cos(lAngle * 0.5f));

            mRotatedIKHintOffset = lYawRotation * mIKHintOffset;
        }

        /*******************************************/
        public Vector3 GetRotatedIKHint()
        {
            return mRotatedIKHintOffset;
        }

        /*******************************************/
        public void SetTargetFootWeight(float weight)
        {
            mTargetFootWeight = weight;
        }

        /*********************************/
        public float GetTargetFootWeight()
        {
            return mTargetFootWeight;
        }

        /*******************************************/
        public void SetCurrentFootWeight(float weight)
        {
            mCurrentFootWeight = weight;
        }

        /*********************************/
        public float GetCurrentFootWeight()
        {
            return mCurrentFootWeight;
        }

        /*******************************************/
        public void SetGoalBlendSpeed(float speed)
        {
            mGoalBlendSpeed = speed;
        }

        /*********************************/
        public float GetGoalBlendSpeed()
        {
            return mGoalBlendSpeed;
        }

        /*********************************/
        public float GetPlantBlendFactor()
        {
            return mPlantBlendFactor;
        }

        /*******************************************/
        public void SetPlantBlendFactor(float factor)
        {
            mPlantBlendFactor = factor;
        }

        /*********************************************/
        public void EnablePlantBlend(float blend_speed)
        {
            mFootPlantBlendSpeed = Mathf.Abs(blend_speed);
            mFootPlantIsOnTransition = true;
        }

        /******************************************************************/
        public void DisablePlantBlend(float blend_speed)
        {
            mFootPlantBlendSpeed = -Mathf.Abs(blend_speed);
            mFootPlantIsOnTransition = true;
        }

        /**********************************/
        public float GetFootPlantBlendSpeed()
        {
            return mFootPlantBlendSpeed;
        }

        /***********************************/
        public void PlantBlendTransitionEnded()
        {
            mFootPlantIsOnTransition = false;
        }

        /***********************************/
        public bool IsPlantOnTransition()
        {
            return mFootPlantIsOnTransition;
        }

        /**************************************/
        public void SetFootPlanted(bool planted)
        {
            mFootPlanted = planted;
        }

        /***************************/
        public bool GetFootPlanted()
        {
            return mFootPlanted;
        }

        /********************************************/
        public void SetPlantedPos(Vector3 planted_pos)
        {
            mFootPlantedPos = planted_pos;
        }

        /*****************************/
        public Vector3 GetPlantedPos()
        {
            return mFootPlantedPos;
        }

        /*******************************************/
        public void SetPlantedRot(Quaternion planted_rot)
        {
            mFootPlantedRot = planted_rot;
        }

        /*******************************/
        public Quaternion GetPlantedRot()
        {
            return mFootPlantedRot;
        }

        /*******************************/
        public void Start()
        {
            _playerBodyAnimator = GetComponent<Animator>();

            if (_playerBodyAnimator == null)
            {
                return;
            }

            HumanBodyBones lBone = HumanBodyBones.LeftFoot;

            switch (mFootID)
            {
                case LimbID.RIGHT_FOOT:
                    lBone = HumanBodyBones.RightFoot;
                    break;

                case LimbID.RIGHT_HAND:
                    lBone = HumanBodyBones.RightHand;
                    break;

                case LimbID.LEFT_HAND:
                    lBone = HumanBodyBones.LeftHand;
                    break;
            }

            mPreviousFootPos = _playerBodyAnimator.GetBoneTransform(lBone).position;
        }

        /*********************************************************************************************/
        protected bool IsErrorHigh(HumanBodyBones bone, Vector3 current_position, Vector3 previous_pos)
        {
            float lErr = (previous_pos - current_position).magnitude;

            float lErrThreshold = mErrorThreshold;

            if (Time.deltaTime < 0.033f) //1/60 seconds
            {
                lErrThreshold = mErrorThreshold * 30 * Time.deltaTime;
            }

            if (lErr > lErrThreshold)
            {
                return true;
            }

            return false;
        }


        /***************************************************/
        void OnAnimatorIK()
        {
            //to check foot stability
            if (!mSetExtraRaydistanceCheckAutomatically)
            {
                return;
            }

            if (_playerBodyAnimator == null)
            {
                return;
            }

            HumanBodyBones lBone = HumanBodyBones.LeftFoot;

            switch (mFootID)
            {
                case LimbID.RIGHT_FOOT:
                    lBone = HumanBodyBones.RightFoot;
                    break;

                case LimbID.RIGHT_HAND:
                    lBone = HumanBodyBones.RightHand;
                    break;

                case LimbID.LEFT_HAND:
                    lBone = HumanBodyBones.LeftHand;
                    break;
            }

            if (IsErrorHigh(lBone, _playerBodyAnimator.GetBoneTransform(lBone).position, mPreviousFootPos))
            {
                mExtraRayDistanceCheck = mExtraRayDistanceCheckMin;
            }
            else
            {
                mExtraRayDistanceCheck = mExtraRayDistanceCheckMax;
            }

            mPreviousFootPos = _playerBodyAnimator.GetBoneTransform(lBone).position;
        }
    }
}