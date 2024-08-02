using UnityEngine;
using System.Collections;

namespace PlayerBodySystem
{
    /// <summary>
    /// This is just a straight copy from AngryNoobs/Jerry's original setup.
    /// I don't exactly know what this does.
    /// </summary>
    public class PlayerBodyFootPlacer : MonoBehaviour
    {
        [Header("This component provides the foot placement functionality.")]
        [Header("It tries to keep the feet touching the ground.")]
        [Header("You can try changing settings if your feet aren't being correctly planted on the ground.")]
        private Animator _playerBodyAnimator;

        public bool mAdjustPelvisVertically = false;
        public bool mDampPelvis = false;
        public float mMaxLegLength = 1.0f;
        public float mMinLegLength = 0.2f;
        public float mPelvisAdjustmentSpeed = 1.0f;
        public string[] mLayersToIgnore;

        //Protected member variables
        //protected FootPlacementData[] mLimbs = {null, null, null, null };
        protected PlayerBodyFootPlacementData mLeftFoot = null;
        protected PlayerBodyFootPlacementData mRightFoot = null;
        protected PlayerBodyFootPlacementData mLeftHand = null;
        protected PlayerBodyFootPlacementData mRightHand = null;
        protected LayerMask mLayerMask = ~0;

        //private member variables
        private const float mEpsilon = 0.005f;
        private float mCurrentRootVertError = 0;
        private float mTargetRootVertError = 0;
        private float mCurrentPelvisSpeed = 0;

        private Vector3 mLeftFootContact_Ontransition_Disable;
        private Vector3 mLeftToeContact_Ontransition_Disable;
        private Vector3 mLeftHeelContact_Ontransition_Disable;


        private Vector3 mRightFootContact_Ontransition_Disable;
        private Vector3 mRightToeContact_Ontransition_Disable;
        private Vector3 mRightHeelContact_Ontransition_Disable;

        private Vector3 mLeftHandContact_Ontransition_Disable;
        private Vector3 mLeftHandToeContact_Ontransition_Disable;
        private Vector3 mLeftHandHeelContact_Ontransition_Disable;

        private Vector3 mRightHandContact_Ontransition_Disable;
        private Vector3 mRightHandToeContact_Ontransition_Disable;
        private Vector3 mRightHandHeelContact_Ontransition_Disable;

        private bool mRootPosLeftRightFoot = false;

        private bool mLeftFootActive = true;
        private bool mRightFootActive = true;
        private bool mLeftHandActive = true;
        private bool mRightHandActive = true;

        //Functions
        /******************************************************/
        public void SetActive(AvatarIKGoal foot_id, bool active)
        {
            //
            if (foot_id == AvatarIKGoal.LeftFoot)
            {
                if (mLeftFoot == null)
                {
                    return;
                }

                if (active)
                {
                    if (!mLeftFootActive)
                    {
                        ResetIKParams(foot_id);
                    }
                }
                mLeftFootActive = active;
            }

            //
            if (foot_id == AvatarIKGoal.RightFoot)
            {
                if (mRightFoot == null)
                {
                    return;
                }

                if (active)
                {
                    if (!mRightFootActive)
                    {
                        ResetIKParams(foot_id);
                    }
                }
                mRightFootActive = active;
            }

            //
            if (foot_id == AvatarIKGoal.LeftHand)
            {
                if (mRightHand == null)
                {
                    return;
                }

                if (active)
                {
                    if (!mLeftHandActive)
                    {
                        ResetIKParams(foot_id);
                    }
                }
                mLeftHandActive = active;
            }

            //
            if (foot_id == AvatarIKGoal.RightHand)
            {
                if (mRightHand == null)
                {
                    return;
                }

                if (active)
                {
                    if (!mRightHandActive)
                    {
                        ResetIKParams(foot_id);
                    }
                }
                mRightHandActive = active;
            }
        }

        /****************************************/
        public bool IsActive(AvatarIKGoal foot_id)
        {

            if (foot_id == AvatarIKGoal.LeftFoot)
            {
                return mLeftFootActive;
            }

            if (foot_id == AvatarIKGoal.RightFoot)
            {
                return mRightFootActive;
            }

            if (foot_id == AvatarIKGoal.LeftHand)
            {
                return mLeftHandActive;
            }

            if (foot_id == AvatarIKGoal.RightHand)
            {
                return mRightHandActive;
            }

            return false;
        }


        /********************************************/
        public void SetLayerMask(LayerMask layer_mask)
        {
            mLayerMask = layer_mask;
        }

        /********************************************/
        public LayerMask GetLayerMask()
        {
            return mLayerMask;
        }


        /*****************************************************/
        public float GetPlantBlendWeight(AvatarIKGoal foot_id)
        {
            PlayerBodyFootPlacementData lFoot;

            switch (foot_id)
            {
                case AvatarIKGoal.LeftFoot:
                    lFoot = mLeftFoot;
                    break;

                case AvatarIKGoal.RightFoot:
                    lFoot = mRightFoot;
                    break;

                case AvatarIKGoal.LeftHand:
                    lFoot = mLeftHand;
                    break;

                case AvatarIKGoal.RightHand:
                    lFoot = mRightHand;
                    break;

                default:
                    return -1;
            }

            return lFoot.GetPlantBlendFactor();
        }

        /******************************************************************/
        public void SetPlantBlendWeight(AvatarIKGoal foot_id, float weight)
        {
            PlayerBodyFootPlacementData lFoot;

            switch (foot_id)
            {
                case AvatarIKGoal.LeftFoot:
                    lFoot = mLeftFoot;
                    break;

                case AvatarIKGoal.RightFoot:
                    lFoot = mRightFoot;
                    break;

                case AvatarIKGoal.LeftHand:
                    lFoot = mLeftHand;
                    break;

                case AvatarIKGoal.RightHand:
                    lFoot = mRightHand;
                    break;

                default:
                    return;
            }

            lFoot.SetPlantBlendFactor(weight);
        }

        /***************************************************************/
        public void EnablePlant(AvatarIKGoal foot_id, float blend_speed)
        {
            PlayerBodyFootPlacementData lFoot;

            switch (foot_id)
            {
                case AvatarIKGoal.LeftFoot:
                    lFoot = mLeftFoot;
                    break;

                case AvatarIKGoal.RightFoot:
                    lFoot = mRightFoot;
                    break;

                case AvatarIKGoal.LeftHand:
                    lFoot = mLeftHand;
                    break;

                case AvatarIKGoal.RightHand:
                    lFoot = mRightHand;
                    break;

                default:
                    return;
            }

            lFoot.EnablePlantBlend(blend_speed);
        }

        /***************************************************************/
        public void DisablePlant(AvatarIKGoal foot_id, float blend_speed)
        {
            PlayerBodyFootPlacementData lFoot;

            switch (foot_id)
            {
                case AvatarIKGoal.LeftFoot:
                    lFoot = mLeftFoot;
                    break;

                case AvatarIKGoal.RightFoot:
                    lFoot = mRightFoot;
                    break;

                case AvatarIKGoal.LeftHand:
                    lFoot = mLeftHand;
                    break;

                case AvatarIKGoal.RightHand:
                    lFoot = mRightHand;
                    break;

                default:
                    return;
            }

            lFoot.DisablePlantBlend(blend_speed);
        }

        /**********************************************/
        private void ResetIKParams(AvatarIKGoal foot_id)
        {

            if (foot_id == AvatarIKGoal.LeftFoot)
            {
                Vector3 lFootPos = _playerBodyAnimator.GetBoneTransform(HumanBodyBones.LeftFoot).position;

                mLeftFoot.SetTargetFootWeight(0);
                mLeftFoot.SetCurrentFootWeight(0);
                mLeftFoot.SetGoalBlendSpeed(0);
                mLeftFoot.SetFootPlanted(false);

                mLeftFootContact_Ontransition_Disable = lFootPos;
                mLeftToeContact_Ontransition_Disable = mLeftFoot.mUpVector * mLeftFoot.mFootOffsetDist + mLeftFoot.GetRotatedFwdVec() * mLeftFoot.mFootLength;
                mLeftHeelContact_Ontransition_Disable = lFootPos + new Quaternion(0, 0.7071f, 0, 0.7071f) * mLeftFoot.GetRotatedFwdVec() * mLeftFoot.mFootHalfWidth;

            }

            if (foot_id == AvatarIKGoal.RightFoot)
            {
                Vector3 lFootPos = _playerBodyAnimator.GetBoneTransform(HumanBodyBones.RightFoot).position;

                mRightFoot.SetTargetFootWeight(0);
                mRightFoot.SetCurrentFootWeight(0);
                mRightFoot.SetGoalBlendSpeed(0);
                mRightFoot.SetFootPlanted(false);

                mRightFootContact_Ontransition_Disable = lFootPos;
                mRightToeContact_Ontransition_Disable = mRightFoot.mUpVector * mRightFoot.mFootOffsetDist + mRightFoot.GetRotatedFwdVec() * mRightFoot.mFootLength;
                mRightHeelContact_Ontransition_Disable = lFootPos + new Quaternion(0, 0.7017f, 0, 0.7071f) * mRightFoot.GetRotatedFwdVec() * mRightFoot.mFootHalfWidth;
            }

            if (foot_id == AvatarIKGoal.LeftHand)
            {
                Vector3 lFootPos = _playerBodyAnimator.GetBoneTransform(HumanBodyBones.LeftHand).position;

                mLeftHand.SetTargetFootWeight(0);
                mLeftHand.SetCurrentFootWeight(0);
                mLeftHand.SetGoalBlendSpeed(0);
                mLeftHand.SetFootPlanted(false);

                mLeftHandContact_Ontransition_Disable = lFootPos;
                mLeftHandToeContact_Ontransition_Disable = mLeftHand.mUpVector * mLeftHand.mFootOffsetDist + mLeftHand.GetRotatedFwdVec() * mLeftHand.mFootLength;
                mLeftHandHeelContact_Ontransition_Disable = lFootPos + new Quaternion(0, 0.7017f, 0, 0.7071f) * mLeftHand.GetRotatedFwdVec() * mLeftHand.mFootHalfWidth;
            }

            if (foot_id == AvatarIKGoal.RightHand)
            {
                Vector3 lFootPos = _playerBodyAnimator.GetBoneTransform(HumanBodyBones.RightHand).position;

                mRightHand.SetTargetFootWeight(0);
                mRightHand.SetCurrentFootWeight(0);
                mRightHand.SetGoalBlendSpeed(0);
                mRightHand.SetFootPlanted(false);

                mRightHandContact_Ontransition_Disable = lFootPos;
                mRightHandToeContact_Ontransition_Disable = mRightHand.mUpVector * mRightHand.mFootOffsetDist + mRightHand.GetRotatedFwdVec() * mRightHand.mFootLength;
                mRightHandHeelContact_Ontransition_Disable = lFootPos + new Quaternion(0, 0.7017f, 0, 0.7071f) * mRightHand.GetRotatedFwdVec() * mRightHand.mFootHalfWidth;
            }
        }


        /*************************************************************************************************/
        protected void SetIKWeight(AvatarIKGoal foot_id, float target_weight, float transition_time)
        {
            switch (foot_id)
            {
                case AvatarIKGoal.LeftFoot:
                    if (Mathf.Abs(target_weight - mLeftFoot.GetTargetFootWeight()) > mEpsilon)//to create linear motion and the blend speed should not be calculated if the condition is not true
                    {
                        if (transition_time != 0)
                        {
                            mLeftFoot.SetGoalBlendSpeed((target_weight - mLeftFoot.GetCurrentFootWeight()) / transition_time);
                        }
                        else
                        {
                            mLeftFoot.SetGoalBlendSpeed(0.1f);
                            mLeftFoot.SetCurrentFootWeight(target_weight);
                        }
                    }
                    mLeftFoot.SetTargetFootWeight(target_weight);
                    break;


                case AvatarIKGoal.RightFoot:
                    if (Mathf.Abs(target_weight - mRightFoot.GetTargetFootWeight()) > mEpsilon)//to create linear motion and the blend speed should not be calculated if the condition is not true
                    {
                        if (transition_time != 0)
                        {
                            mRightFoot.SetGoalBlendSpeed((target_weight - mRightFoot.GetCurrentFootWeight()) / transition_time);
                        }
                        else
                        {
                            mRightFoot.SetGoalBlendSpeed(0.1f);
                            mRightFoot.SetCurrentFootWeight(target_weight);
                        }
                    }
                    mRightFoot.SetTargetFootWeight(target_weight);
                    break;

                case AvatarIKGoal.LeftHand:
                    if (Mathf.Abs(target_weight - mLeftHand.GetTargetFootWeight()) > mEpsilon)//to create linear motion and the blend speed should not be calculated if the condition is not true
                    {
                        if (transition_time != 0)
                        {
                            mLeftHand.SetGoalBlendSpeed((target_weight - mLeftHand.GetCurrentFootWeight()) / transition_time);
                        }
                        else
                        {
                            mLeftHand.SetGoalBlendSpeed(0.1f);
                            mLeftHand.SetCurrentFootWeight(target_weight);
                        }
                    }
                    mLeftHand.SetTargetFootWeight(target_weight);
                    break;

                case AvatarIKGoal.RightHand:
                    if (Mathf.Abs(target_weight - mRightHand.GetTargetFootWeight()) > mEpsilon)//to create linear motion and the blend speed should not be calculated if the condition is not true
                    {
                        if (transition_time != 0)
                        {
                            mRightHand.SetGoalBlendSpeed((target_weight - mRightHand.GetCurrentFootWeight()) / transition_time);
                        }
                        else
                        {
                            mRightHand.SetGoalBlendSpeed(0.1f);
                            mRightHand.SetCurrentFootWeight(target_weight);
                        }
                    }
                    mRightHand.SetTargetFootWeight(target_weight);
                    break;
            }
        }

        /*********************************************************/
        protected void CalculateIKGoalWeights(AvatarIKGoal foot_id)
        {
            PlayerBodyFootPlacementData lFoot;

            switch (foot_id)
            {
                case AvatarIKGoal.LeftFoot:
                    lFoot = mLeftFoot;
                    break;

                case AvatarIKGoal.RightFoot:
                    lFoot = mRightFoot;
                    break;

                case AvatarIKGoal.LeftHand:
                    lFoot = mLeftHand;
                    break;

                case AvatarIKGoal.RightHand:
                    lFoot = mRightHand;
                    break;

                default:
                    return;
            }


            float lSign = Mathf.Sign(lFoot.GetTargetFootWeight() - lFoot.GetCurrentFootWeight());
            lFoot.SetCurrentFootWeight(lFoot.GetCurrentFootWeight() + lFoot.GetGoalBlendSpeed() * Time.deltaTime);

            if (lSign * Mathf.Sign(lFoot.GetTargetFootWeight() - lFoot.GetCurrentFootWeight()) < 1 ||
              Mathf.Abs(lFoot.GetCurrentFootWeight() - lFoot.GetTargetFootWeight()) < mEpsilon)
            {
                lFoot.SetCurrentFootWeight(lFoot.GetTargetFootWeight());
                return;
            }

            if (lFoot.GetCurrentFootWeight() > 1 || lFoot.GetCurrentFootWeight() < 0)
            {
                lFoot.SetCurrentFootWeight(Mathf.Clamp(lFoot.GetTargetFootWeight(), 0, 1));
            }
        }

        /*************************************************/
        protected void CheckForLimits(AvatarIKGoal foot_id)
        {
            PlayerBodyFootPlacementData lFoot;

            switch (foot_id)
            {
                case AvatarIKGoal.LeftFoot:
                    lFoot = mLeftFoot;
                    break;

                case AvatarIKGoal.RightFoot:
                    lFoot = mRightFoot;
                    break;

                case AvatarIKGoal.LeftHand:
                    lFoot = mLeftHand;
                    break;

                case AvatarIKGoal.RightHand:
                    lFoot = mRightHand;
                    break;

                default:
                    return;

            }

            //To check pitch limit of foot
            Vector3 lExtraOffset = Vector3.zero;
            Vector3 lRotatedFwdVec = lFoot.GetRotatedFwdVec();

            if (Vector3.Angle(lRotatedFwdVec, lFoot.GetTargetPos(PlayerBodyFootPlacementData.Target.TOE)) > lFoot.mFootRotationLimit)
            {
                lExtraOffset = lFoot.mUpVector * lFoot.mFootLength * Mathf.Tan(lFoot.mFootRotationLimit * (Mathf.PI / 180));

                if (Vector3.Angle(lFoot.mUpVector, lFoot.GetTargetPos(PlayerBodyFootPlacementData.Target.TOE)) > 90)
                {
                    lExtraOffset = -lExtraOffset;
                }

                lFoot.SetTargetPos(PlayerBodyFootPlacementData.Target.TOE, (lRotatedFwdVec * lFoot.mFootLength) + lExtraOffset);
            }

            Quaternion lQuat90 = new Quaternion(0, 0.7071f, 0, 0.7071f);

            //To check roll limit of foot
            if (Vector3.Angle(lQuat90 * lRotatedFwdVec, lFoot.GetTargetPos(PlayerBodyFootPlacementData.Target.HEEL)) > lFoot.mFootRotationLimit)
            {
                lExtraOffset = lFoot.mUpVector * lFoot.mFootHalfWidth * Mathf.Tan(lFoot.mFootRotationLimit * (Mathf.PI / 180));

                if (Vector3.Angle(lFoot.mUpVector, lFoot.GetTargetPos(PlayerBodyFootPlacementData.Target.HEEL)) > 90)
                {
                    lExtraOffset = -lExtraOffset;
                }
                lFoot.SetTargetPos(PlayerBodyFootPlacementData.Target.HEEL, (lQuat90 * lRotatedFwdVec * lFoot.mFootHalfWidth) + lExtraOffset);
            }
        }

        /***************************************************************/
        protected void UpdateFootPlantBlendWeights(AvatarIKGoal foot_id)
        {
            PlayerBodyFootPlacementData lFoot;

            switch (foot_id)
            {
                case AvatarIKGoal.LeftFoot:
                    lFoot = mLeftFoot;
                    break;

                case AvatarIKGoal.RightFoot:
                    lFoot = mRightFoot;
                    break;

                case AvatarIKGoal.LeftHand:
                    lFoot = mLeftHand;
                    break;

                case AvatarIKGoal.RightHand:
                    lFoot = mRightHand;
                    break;
                default:
                    return;
            }

            if (lFoot.IsPlantOnTransition())
            {
                lFoot.SetPlantBlendFactor(lFoot.GetPlantBlendFactor() + lFoot.GetFootPlantBlendSpeed() * Time.deltaTime);

                if (lFoot.GetFootPlantBlendSpeed() > 0) //enbale foot plant
                {
                    if (lFoot.GetPlantBlendFactor() > 1)
                    {
                        lFoot.SetPlantBlendFactor(1);
                        lFoot.PlantBlendTransitionEnded();
                    }
                }
                else
                {
                    if (lFoot.GetPlantBlendFactor() < 0) // disable foot plant
                    {
                        lFoot.SetPlantBlendFactor(0);
                        lFoot.PlantBlendTransitionEnded();
                    }
                }

            }
        }

        /****************************************************/
        protected void FindContactPoints(AvatarIKGoal foot_id)
        {
            PlayerBodyFootPlacementData lFoot;
            Vector3 lContactPos;
            Vector3 lFootPos = _playerBodyAnimator.GetIKPosition(foot_id);
            bool lContactDetected = true;


            switch (foot_id)
            {
                case AvatarIKGoal.LeftFoot:
                    lFoot = mLeftFoot;
                    break;

                case AvatarIKGoal.RightFoot:
                    lFoot = mRightFoot;
                    break;

                case AvatarIKGoal.LeftHand:
                    lFoot = mLeftHand;
                    break;

                case AvatarIKGoal.RightHand:
                    lFoot = mRightHand;
                    break;

                default:
                    return;
            }

            RaycastHit lHit;

            /*******************************************************************************************/
            if (Physics.Raycast(lFootPos + lFoot.mUpVector * lFoot.mFootOffsetDist, -lFoot.mUpVector, out lHit, lFoot.mFootOffsetDist + lFoot.mFootHeight + lFoot.mExtraRayDistanceCheck, mLayerMask))
            {
                SetIKWeight(foot_id, 1, lFoot.mTransitionTime);

                {//Scope Start For lResult Var
                    Vector3 lResult = lHit.point;

                    if (lFoot.mPlantFoot && lFoot.GetFootPlanted())
                    {
                        if (Physics.Raycast(lFoot.GetPlantedPos() + lFoot.mUpVector * lFoot.mFootOffsetDist, -lFoot.mUpVector, out lHit, lFoot.mFootOffsetDist + lFoot.mFootHeight + lFoot.mExtraRayDistanceCheck, mLayerMask))
                        {
                            lResult = Vector3.Lerp(lResult, lHit.point, lFoot.GetPlantBlendFactor());
                        }
                    }

                    lFoot.SetTargetPos(PlayerBodyFootPlacementData.Target.FOOT, lResult);
                }//Scope End


                switch (foot_id)
                {
                    case AvatarIKGoal.LeftFoot:
                        mLeftFootContact_Ontransition_Disable = lFoot.GetTargetPos(PlayerBodyFootPlacementData.Target.FOOT);
                        break;

                    case AvatarIKGoal.RightFoot:
                        mRightFootContact_Ontransition_Disable = lFoot.GetTargetPos(PlayerBodyFootPlacementData.Target.FOOT);
                        break;

                    case AvatarIKGoal.LeftHand:
                        mLeftHandContact_Ontransition_Disable = lFoot.GetTargetPos(PlayerBodyFootPlacementData.Target.FOOT);
                        break;

                    case AvatarIKGoal.RightHand:
                        mRightHandContact_Ontransition_Disable = lFoot.GetTargetPos(PlayerBodyFootPlacementData.Target.FOOT);
                        break;
                }

                lContactDetected = true;

            }
            else
            {
                SetIKWeight(foot_id, 0, lFoot.mTransitionTime);

                switch (foot_id)
                {
                    case AvatarIKGoal.LeftFoot:
                        lContactPos = mLeftFootContact_Ontransition_Disable;
                        break;

                    case AvatarIKGoal.RightFoot:
                        lContactPos = mRightFootContact_Ontransition_Disable;
                        break;

                    case AvatarIKGoal.LeftHand:
                        lContactPos = mLeftHandContact_Ontransition_Disable;
                        break;

                    case AvatarIKGoal.RightHand:
                        lContactPos = mRightHandContact_Ontransition_Disable;
                        break;

                    default:
                        lContactPos = Vector3.zero;
                        break;
                }

                lFoot.SetTargetPos(PlayerBodyFootPlacementData.Target.FOOT, Vector3.Lerp(lFootPos, lContactPos, lFoot.GetCurrentFootWeight()));
                lContactDetected = false;
            }

            if (Physics.Raycast(lFootPos + (lFoot.mUpVector * lFoot.mFootOffsetDist) + (lFoot.GetRotatedFwdVec() * lFoot.mFootLength), -lFoot.mUpVector, out lHit,
                               lFoot.mFootOffsetDist + lFoot.mFootLength + lFoot.mExtraRayDistanceCheck, mLayerMask) && lContactDetected)
            {
                {//Scope Start For lResult Var
                    Vector3 lResult = lHit.point;
                    if (lFoot.mPlantFoot && lFoot.GetFootPlanted())
                    {
                        if (Physics.Raycast(lFoot.GetPlantedPos() + (lFoot.mUpVector * lFoot.mFootOffsetDist) + (lFoot.GetRotatedFwdVec() * lFoot.mFootLength), -lFoot.mUpVector, out lHit,
                                        lFoot.mFootOffsetDist + lFoot.mFootLength + lFoot.mExtraRayDistanceCheck, mLayerMask))
                        {
                            lResult = Vector3.Lerp(lResult, lHit.point, lFoot.GetPlantBlendFactor());
                        }
                    }

                    lFoot.SetTargetPos(PlayerBodyFootPlacementData.Target.TOE, lResult - lFoot.GetTargetPos(PlayerBodyFootPlacementData.Target.FOOT));
                }//Scope End            

                switch (foot_id)
                {
                    case AvatarIKGoal.LeftFoot:
                        mLeftToeContact_Ontransition_Disable = lFoot.GetTargetPos(PlayerBodyFootPlacementData.Target.TOE);
                        break;

                    case AvatarIKGoal.RightFoot:
                        mRightToeContact_Ontransition_Disable = lFoot.GetTargetPos(PlayerBodyFootPlacementData.Target.TOE);
                        break;

                    case AvatarIKGoal.LeftHand:
                        mLeftHandToeContact_Ontransition_Disable = lFoot.GetTargetPos(PlayerBodyFootPlacementData.Target.TOE);
                        break;

                    case AvatarIKGoal.RightHand:
                        mRightHandToeContact_Ontransition_Disable = lFoot.GetTargetPos(PlayerBodyFootPlacementData.Target.TOE);
                        break;
                }


            }
            else
            {
                switch (foot_id)
                {
                    case AvatarIKGoal.LeftFoot:
                        lContactPos = mLeftToeContact_Ontransition_Disable;
                        break;

                    case AvatarIKGoal.RightFoot:
                        lContactPos = mRightToeContact_Ontransition_Disable;
                        break;

                    case AvatarIKGoal.LeftHand:
                        lContactPos = mLeftHandToeContact_Ontransition_Disable;
                        break;

                    case AvatarIKGoal.RightHand:
                        lContactPos = mRightHandToeContact_Ontransition_Disable;
                        break;

                    default:
                        lContactPos = Vector3.zero;
                        break;
                }

                lFoot.SetTargetPos(PlayerBodyFootPlacementData.Target.TOE,
                                   Vector3.Slerp(lFoot.GetRotatedFwdVec() * lFoot.mFootLength, lContactPos, lFoot.GetCurrentFootWeight()));
            }

            Quaternion lQuat90 = new Quaternion(0, 0.7071f, 0, 0.7071f);

            if (Physics.Raycast(lFootPos + (lFoot.mUpVector * lFoot.mFootOffsetDist) + ((lQuat90 * lFoot.GetRotatedFwdVec()).normalized * lFoot.mFootHalfWidth), -lFoot.mUpVector, out lHit,
                               lFoot.mFootOffsetDist + lFoot.mFootLength + lFoot.mExtraRayDistanceCheck, mLayerMask) && lContactDetected)
            {
                {//Scope Start For lResult Var
                    Vector3 lResult = lHit.point;

                    if (lFoot.mPlantFoot && lFoot.GetFootPlanted())
                    {
                        if (Physics.Raycast(lFoot.GetPlantedPos() + (lFoot.mUpVector * lFoot.mFootOffsetDist) + ((lQuat90 * lFoot.GetRotatedFwdVec()).normalized * lFoot.mFootHalfWidth), -lFoot.mUpVector, out lHit,
                                        lFoot.mFootOffsetDist + lFoot.mFootLength + lFoot.mExtraRayDistanceCheck, mLayerMask))
                        {
                            lResult = Vector3.Lerp(lResult, lHit.point, lFoot.GetPlantBlendFactor());
                        }
                    }

                    lFoot.SetTargetPos(PlayerBodyFootPlacementData.Target.HEEL, lResult - lFoot.GetTargetPos(PlayerBodyFootPlacementData.Target.FOOT));
                }//Scope End            

                switch (foot_id)
                {
                    case AvatarIKGoal.LeftFoot:
                        mLeftHeelContact_Ontransition_Disable = lFoot.GetTargetPos(PlayerBodyFootPlacementData.Target.HEEL);
                        break;

                    case AvatarIKGoal.RightFoot:
                        mRightHeelContact_Ontransition_Disable = lFoot.GetTargetPos(PlayerBodyFootPlacementData.Target.HEEL);
                        break;

                    case AvatarIKGoal.LeftHand:
                        mLeftHandHeelContact_Ontransition_Disable = lFoot.GetTargetPos(PlayerBodyFootPlacementData.Target.HEEL);
                        break;

                    case AvatarIKGoal.RightHand:
                        mRightHeelContact_Ontransition_Disable = lFoot.GetTargetPos(PlayerBodyFootPlacementData.Target.HEEL);
                        break;

                    default:
                        lContactPos = Vector3.zero;
                        break;
                }
            }
            else
            {
                switch (foot_id)
                {
                    case AvatarIKGoal.LeftFoot:
                        lContactPos = mLeftHeelContact_Ontransition_Disable;
                        break;

                    case AvatarIKGoal.RightFoot:
                        lContactPos = mRightHeelContact_Ontransition_Disable;
                        break;

                    case AvatarIKGoal.LeftHand:
                        lContactPos = mLeftHandHeelContact_Ontransition_Disable;
                        break;

                    case AvatarIKGoal.RightHand:
                        lContactPos = mRightHandHeelContact_Ontransition_Disable;
                        break;

                    default:
                        lContactPos = Vector3.zero;
                        break;
                }


                lFoot.SetTargetPos(PlayerBodyFootPlacementData.Target.HEEL,
                                   Vector3.Slerp((lQuat90 * lFoot.GetRotatedFwdVec() * lFoot.mFootHalfWidth), lContactPos, lFoot.GetCurrentFootWeight()));
            }
        }


        /************************************/
        protected void RootPositioningCheck()
        {
            AnimateRootVertError();

            if (mAdjustPelvisVertically)
            {
                float lLegLength = (_playerBodyAnimator.GetBoneTransform(HumanBodyBones.RightUpperLeg).position - _playerBodyAnimator.GetIKPosition(AvatarIKGoal.RightFoot)).magnitude;
                PlayerBodyFootPlacementData lFoot = mRightFoot;
                mRootPosLeftRightFoot = true;

                if ((_playerBodyAnimator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position - _playerBodyAnimator.GetIKPosition(AvatarIKGoal.LeftFoot)).sqrMagnitude >
                   (_playerBodyAnimator.GetBoneTransform(HumanBodyBones.RightUpperLeg).position - _playerBodyAnimator.GetIKPosition(AvatarIKGoal.RightFoot)).sqrMagnitude)
                {
                    lLegLength = (_playerBodyAnimator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position - _playerBodyAnimator.GetIKPosition(AvatarIKGoal.LeftFoot)).magnitude;
                    lFoot = mLeftFoot;
                    mRootPosLeftRightFoot = false;
                }

                if (lLegLength > mMaxLegLength)
                {
                    mTargetRootVertError = lLegLength - mMaxLegLength;
                    float lLeftLegError = 0;
                    float lRightLegError = 0;
                    float lBuff = (_playerBodyAnimator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position - (_playerBodyAnimator.GetIKPosition(AvatarIKGoal.LeftFoot) + mLeftFoot.mUpVector * mTargetRootVertError)).magnitude;


                    if (lBuff < mMinLegLength)
                    {
                        lLeftLegError = mMinLegLength - lBuff;
                    }

                    lBuff = (_playerBodyAnimator.GetBoneTransform(HumanBodyBones.RightUpperLeg).position - (_playerBodyAnimator.GetIKPosition(AvatarIKGoal.RightFoot) + mLeftFoot.mUpVector * mTargetRootVertError)).magnitude;

                    if (lBuff < mMinLegLength)
                    {
                        lRightLegError = mMinLegLength - lBuff;
                    }

                    if (lRightLegError != 0 || lLeftLegError != 0)
                    {
                        if (lRightLegError > lLeftLegError)
                        {
                            mTargetRootVertError -= lRightLegError;
                        }
                        else
                        {
                            mTargetRootVertError -= lLeftLegError;
                        }
                    }
                }
                else
                {
                    mTargetRootVertError = 0;
                    mCurrentPelvisSpeed = 0;
                }


                _playerBodyAnimator.SetIKPosition(AvatarIKGoal.LeftFoot, _playerBodyAnimator.GetIKPosition(AvatarIKGoal.LeftFoot) + lFoot.mUpVector * mCurrentRootVertError);
                _playerBodyAnimator.SetIKPosition(AvatarIKGoal.RightFoot, _playerBodyAnimator.GetIKPosition(AvatarIKGoal.RightFoot) + lFoot.mUpVector * mCurrentRootVertError);

                _playerBodyAnimator.SetIKPosition(AvatarIKGoal.LeftHand, _playerBodyAnimator.GetIKPosition(AvatarIKGoal.LeftHand) + lFoot.mUpVector * mCurrentRootVertError);
                _playerBodyAnimator.SetIKPosition(AvatarIKGoal.RightHand, _playerBodyAnimator.GetIKPosition(AvatarIKGoal.RightHand) + lFoot.mUpVector * mCurrentRootVertError);


            }
            else
            {
                mTargetRootVertError = 0;
                mCurrentPelvisSpeed = 0;

                if (Mathf.Abs(mCurrentRootVertError) >= mEpsilon)
                {
                    if (mRootPosLeftRightFoot)
                    {
                        _playerBodyAnimator.SetIKPosition(AvatarIKGoal.LeftFoot, _playerBodyAnimator.GetIKPosition(AvatarIKGoal.LeftFoot) + mRightFoot.mUpVector * mCurrentRootVertError);
                        _playerBodyAnimator.SetIKPosition(AvatarIKGoal.RightFoot, _playerBodyAnimator.GetIKPosition(AvatarIKGoal.RightFoot) + mRightFoot.mUpVector * mCurrentRootVertError);

                        _playerBodyAnimator.SetIKPosition(AvatarIKGoal.LeftHand, _playerBodyAnimator.GetIKPosition(AvatarIKGoal.LeftHand) + mRightFoot.mUpVector * mCurrentRootVertError);
                        _playerBodyAnimator.SetIKPosition(AvatarIKGoal.RightHand, _playerBodyAnimator.GetIKPosition(AvatarIKGoal.RightHand) + mRightFoot.mUpVector * mCurrentRootVertError);

                    }
                    else
                    {
                        _playerBodyAnimator.SetIKPosition(AvatarIKGoal.LeftFoot, _playerBodyAnimator.GetIKPosition(AvatarIKGoal.LeftFoot) + mRightFoot.mUpVector * mCurrentRootVertError);
                        _playerBodyAnimator.SetIKPosition(AvatarIKGoal.RightFoot, _playerBodyAnimator.GetIKPosition(AvatarIKGoal.RightFoot) + mRightFoot.mUpVector * mCurrentRootVertError);

                        _playerBodyAnimator.SetIKPosition(AvatarIKGoal.LeftHand, _playerBodyAnimator.GetIKPosition(AvatarIKGoal.LeftHand) + mRightFoot.mUpVector * mCurrentRootVertError);
                        _playerBodyAnimator.SetIKPosition(AvatarIKGoal.RightHand, _playerBodyAnimator.GetIKPosition(AvatarIKGoal.RightHand) + mRightFoot.mUpVector * mCurrentRootVertError);

                    }
                }
            }
        }

        /*******************************************************/
        protected void AnimateRootVertError()
        {
            float lErrorDiff = Mathf.Abs(mCurrentRootVertError - mTargetRootVertError);

            if (lErrorDiff < mEpsilon)
            {
                mCurrentRootVertError = mTargetRootVertError;
                mCurrentPelvisSpeed = 0;
            }
            else
            {
                float lSign = Mathf.Sign(mTargetRootVertError - mCurrentRootVertError);

                if (mDampPelvis)
                {
                    if (lErrorDiff < mTargetRootVertError * 0.3f)
                    {
                        mCurrentPelvisSpeed -= mPelvisAdjustmentSpeed * Time.deltaTime;

                        if (mCurrentPelvisSpeed < mPelvisAdjustmentSpeed * 0.5f)
                        {
                            mCurrentPelvisSpeed = mPelvisAdjustmentSpeed * 0.5f;
                        }

                        mCurrentRootVertError += lSign * mCurrentPelvisSpeed * Time.deltaTime;
                    }
                    else
                    {
                        mCurrentRootVertError += lSign * mPelvisAdjustmentSpeed * Time.deltaTime;
                    }
                }
                else
                {
                    mCurrentRootVertError += lSign * mPelvisAdjustmentSpeed * Time.deltaTime;
                    mCurrentPelvisSpeed = 0;
                }

                if (Mathf.Sign(mTargetRootVertError - mCurrentRootVertError) * lSign <= 0)
                {
                    mCurrentRootVertError = mTargetRootVertError;
                }
            }
        }

        /*********************************************/
        public void FootPlacement(AvatarIKGoal foot_id)
        {

            PlayerBodyFootPlacementData lFoot = null;

            switch (foot_id)
            {
                case AvatarIKGoal.LeftFoot:
                    if (!mLeftFootActive)
                    {
                        return;
                    }
                    lFoot = mLeftFoot;
                    break;

                case AvatarIKGoal.RightFoot:
                    if (!mRightFootActive)
                    {
                        return;
                    }
                    lFoot = mRightFoot;
                    break;

                case AvatarIKGoal.LeftHand:
                    if (!mLeftHandActive)
                    {
                        return;
                    }
                    lFoot = mLeftHand;
                    break;

                case AvatarIKGoal.RightHand:
                    if (!mRightHandActive)
                    {
                        return;
                    }
                    lFoot = mRightHand;
                    break;

                default:
                    return;
            }

            lFoot.mUpVector.Normalize();
            lFoot.mForwardVector.Normalize();

            //Update forward vec and IKHintoffset based on cahracter foot and body rotation
            lFoot.CalculateRotatedIKHint();
            lFoot.CalculateRotatedFwdVec();

            lFoot.SetTargetPos(PlayerBodyFootPlacementData.Target.FOOT, _playerBodyAnimator.GetIKPosition(foot_id));

            //Updating Plant Foot Blend Transitions
            UpdateFootPlantBlendWeights(foot_id);

            //Find exact contact points
            FindContactPoints(foot_id);

            //Check for feet limits
            CheckForLimits(foot_id);

            //Update IK goal weights
            CalculateIKGoalWeights(foot_id);

            /*Setup Final IK Rotaiton*/
            _playerBodyAnimator.SetIKRotationWeight(foot_id, lFoot.GetCurrentFootWeight());


            //converting the foot yaw rotation to degree
            float lAngle = Vector3.Angle(lFoot.GetRotatedFwdVec(), lFoot.mForwardVector);

            Quaternion lQuat90 = new Quaternion(0, 0.7071f, 0, 0.7071f);
            Vector3 lEulerRot = Quaternion.FromToRotation(lFoot.mForwardVector, lFoot.GetTargetPos(PlayerBodyFootPlacementData.Target.TOE)).eulerAngles;
            lEulerRot.z = 0;
            Quaternion lRot = Quaternion.Euler(lEulerRot);
            if ((lAngle > 90 && lAngle < 180) || (lAngle > 270 && lAngle < 360))
            {
                lEulerRot = Quaternion.FromToRotation(lFoot.GetTargetPos(PlayerBodyFootPlacementData.Target.HEEL), lQuat90 * lFoot.GetRotatedFwdVec()).eulerAngles;
            }
            else
            {
                lEulerRot = Quaternion.FromToRotation(lQuat90 * lFoot.GetRotatedFwdVec(), lFoot.GetTargetPos(PlayerBodyFootPlacementData.Target.HEEL)).eulerAngles;
            }

            lEulerRot.x = 0;
            lEulerRot.y = 0;
            lRot = lRot * Quaternion.Euler(lEulerRot);
            _playerBodyAnimator.SetIKRotation(foot_id, lRot);

            /*Setup Final IK Swivel Angle*/
            Vector3 lIKHintDir = Vector3.zero;

            if (foot_id == AvatarIKGoal.LeftFoot)
            {
                lIKHintDir = _playerBodyAnimator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position - _playerBodyAnimator.GetIKPosition(AvatarIKGoal.LeftFoot);//mAnim.GetBoneTransform(HumanBodyBones.LeftFoot).position;
                _playerBodyAnimator.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, lFoot.GetCurrentFootWeight());

                _playerBodyAnimator.SetIKHintPosition(AvatarIKHint.LeftKnee, lFoot.GetTargetPos(PlayerBodyFootPlacementData.Target.TOE) + lFoot.GetTargetPos(PlayerBodyFootPlacementData.Target.FOOT)
                                        + lFoot.GetRotatedIKHint() + lIKHintDir);

            }

            if (foot_id == AvatarIKGoal.RightFoot)
            {
                lIKHintDir = _playerBodyAnimator.GetBoneTransform(HumanBodyBones.RightLowerLeg).position - _playerBodyAnimator.GetBoneTransform(HumanBodyBones.RightFoot).position;
                _playerBodyAnimator.SetIKHintPositionWeight(AvatarIKHint.RightKnee, lFoot.GetCurrentFootWeight());
                _playerBodyAnimator.SetIKHintPosition(AvatarIKHint.RightKnee, lFoot.GetTargetPos(PlayerBodyFootPlacementData.Target.TOE) + lFoot.GetTargetPos(PlayerBodyFootPlacementData.Target.FOOT)
                                        + lFoot.GetRotatedIKHint() + lIKHintDir);
            }

            /*Setup Final IK Position*/

            Vector3 lFinalFootPos = lFoot.GetTargetPos(PlayerBodyFootPlacementData.Target.FOOT) + (lFoot.mUpVector * lFoot.mFootHeight);
            _playerBodyAnimator.SetIKPositionWeight(foot_id, lFoot.GetCurrentFootWeight());
            _playerBodyAnimator.SetIKPosition(foot_id, lFinalFootPos);



            /*Update planted foot position and rotation*/
            if (lFoot.GetCurrentFootWeight() <= 0)
            {
                lFoot.SetFootPlanted(false);
            }
            else
            {
                if (lFoot.mPlantFoot && !lFoot.GetFootPlanted())
                {
                    if (Mathf.Abs(lFoot.GetTargetFootWeight() - lFoot.GetCurrentFootWeight()) < mEpsilon)
                    {
                        lFoot.SetPlantedPos(_playerBodyAnimator.GetIKPosition(foot_id));//
                        lFoot.SetPlantedRot(_playerBodyAnimator.GetIKRotation(foot_id));
                        lFoot.SetFootPlanted(true);
                    }
                }
            }
        }

        void UpdateLayerMask()
        {
            for (byte i = 0; i < mLayersToIgnore.Length; i++)
            {
                int lLayerNumber = LayerMask.NameToLayer(mLayersToIgnore[i]);
                if (lLayerNumber != -1)
                {
                    mLayerMask &= ~Mathf.RoundToInt(Mathf.Pow(2, lLayerNumber));
                }
            }
        }

        /***********/
        public void Start()
        {
            _playerBodyAnimator = GetComponent<Animator>();

            bool lLeftFootSet = false;
            bool lRightFootSet = false;
            bool lLeftHandSet = false;
            bool lRightHandSet = false;
            PlayerBodyFootPlacementData[] lFeet = GetComponents<PlayerBodyFootPlacementData>();

            for (byte i = 0; i < lFeet.Length; i++)
            {
                switch (lFeet[i].mFootID)
                {
                    case PlayerBodyFootPlacementData.LimbID.LEFT_FOOT:
                        lLeftFootSet = true;
                        mLeftFoot = lFeet[i];
                        break;

                    case PlayerBodyFootPlacementData.LimbID.RIGHT_FOOT:
                        lRightFootSet = true;
                        mRightFoot = lFeet[i];
                        break;

                    case PlayerBodyFootPlacementData.LimbID.RIGHT_HAND:
                        lRightHandSet = true;
                        mLeftHand = lFeet[i];
                        break;

                    case PlayerBodyFootPlacementData.LimbID.LEFT_HAND:
                        lLeftHandSet = true;
                        mRightHand = lFeet[i];
                        break;

                }

                if (lLeftFootSet && lRightFootSet && lLeftHandSet && lRightHandSet)
                {
                    break;
                }
            }
        }

        /*****************/
        public void OnAnimatorIK()
        {
            UpdateLayerMask();

            if (mLeftFoot != null)
            {
                FootPlacement(AvatarIKGoal.LeftFoot);
            }

            if (mRightFoot != null)
            {
                FootPlacement(AvatarIKGoal.RightFoot);
            }

            if (mLeftHand != null)
            {
                FootPlacement(AvatarIKGoal.LeftHand);
            }

            if (mRightHand != null)
            {
                FootPlacement(AvatarIKGoal.RightHand);
            }

            RootPositioningCheck();

        }

        /***************/
        public void LateUpdate()
        {
            if (mCurrentRootVertError != 0)
            {
                if (mRootPosLeftRightFoot)
                {
                    _playerBodyAnimator.GetBoneTransform(HumanBodyBones.Hips).position -= mRightFoot.mUpVector * mCurrentRootVertError;
                }
                else
                {
                    _playerBodyAnimator.GetBoneTransform(HumanBodyBones.Hips).position -= mLeftFoot.mUpVector * mCurrentRootVertError;
                }
            }
        }
    }
}