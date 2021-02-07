﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using WizardsCode.Stats;

namespace WizardsCode.Character.Stats
{
    /// <summary>
    /// Place the StatsInfluencerTrigger on any game object with a trigger collider. When 
    /// another object with a StatsController attached triggers the collider the defined
    /// StatsInfluencer is attached to the StatsController. That StatsController will then
    /// apply the influence as defined within the StatsInfluencerSO.
    /// </summary>
    public class StatsInfluencerTrigger : MonoBehaviour
    {
        [SerializeField, Tooltip("The name of the interaction that will produce this result.")]
        string m_InteractionName = "Name of Interaction";
        [SerializeField, Tooltip("The set of character stats and the influence to apply to them when a character interacts with the object.")]
        [FormerlySerializedAs("influences")]
        internal StatInfluence[] m_CharacterInfluences;
        [SerializeField, Tooltip("The set of object stats and the influence to apply to them when a character interacts with the object.")]
        internal StatInfluence[] m_ObjectInfluences;
        [SerializeField, Tooltip("The time, in seconds, over which the influencer will be effective. The total change will occure over this time period. If duration is 0 then the total change is applied instantly")]
        float m_Duration = 0;
        [SerializeField, Tooltip("The cooldown time before a character can be influenced by this influencer again.")]
        float m_Cooldown = 30;
        [SerializeField, Tooltip("If the actor stays within the trigger area can they get a new influencer after the duration + cooldown has expired?")]
        bool m_IsRepeating = false;

        private StatsTracker m_StatsTracker;

        /// <summary>
        /// The influences that act upon the interacting character.
        /// </summary>
        internal StatInfluence[] CharacterInfluences{get { return m_CharacterInfluences; } }

        /// <summary>
        /// The influences that act upon the interacting character.
        /// </summary>
        internal StatInfluence[] ObjectInfluences { get { return m_ObjectInfluences; } }

        private void Awake()
        {
            m_StatsTracker = GetComponentInParent<StatsTracker>();
        }

        /// <summary>
        /// Test to see if this influencer trigger is on cooldown for a given actor.
        /// </summary>
        /// <param name="brain">The brain of the actor we are testing against</param>
        /// <returns>True if this influencer is on cooldown, meaning the actor cannot use it yet.</returns>
        internal bool IsOnCooldownFor(Brain brain)
        {
            float lastTime;
            if (m_TimeOfLastInfluence.TryGetValue(brain, out lastTime))
            {
                return Time.timeSinceLevelLoad < lastTime + m_Cooldown;
            } else
            {
                return false;
            }
        }

        private Dictionary<Brain, float> m_TimeOfLastInfluence = new Dictionary<Brain, float>();

        /// <summary>
        /// The name of this interaction. Used as an ID for this interaction.
        /// </summary>
        public string InteractionName
        {
            get { return m_InteractionName; }
            set { m_InteractionName = value; }
        }

        /// <summary>
        /// The time this influencer will operate.
        /// </summary>
        public float Duration { 
            get
            {
                return m_Duration;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject == this.gameObject) return;

            Brain brain = other.GetComponentInParent<Brain>();

            if (brain == null || !brain.ShouldInteractWith(this)) return;
            AddCharacterInfluence(brain);
            AddObjectInfluence();
        }

        private void OnTriggerStay(Collider other)
        {
            if (!m_IsRepeating) return;

            if (other.gameObject == this.gameObject) return;

            Brain brain = other.GetComponentInParent<Brain>();

            if (brain == null || !brain.ShouldInteractWith(this)) return;

            if (!IsOnCooldownFor(brain))
            {
                AddCharacterInfluence(brain);
                AddObjectInfluence();
            }
        }

        private void AddCharacterInfluence(Brain brain)
        {
            for (int i = 0; i < CharacterInfluences.Length; i++)
            {
                StatInfluencerSO influencer = ScriptableObject.CreateInstance<StatInfluencerSO>();
                influencer.InteractionName = CharacterInfluences[i].statTemplate.name + " influencer from " + InteractionName + " (" + GetInstanceID() + ")";
                influencer.generator = gameObject;
                influencer.stat = CharacterInfluences[i].statTemplate;
                influencer.maxChange = CharacterInfluences[i].maxChange;
                influencer.duration = m_Duration;
                influencer.cooldown = m_Cooldown;

                if (brain.TryAddInfluencer(influencer))
                {
                    m_TimeOfLastInfluence.Remove(brain);
                    m_TimeOfLastInfluence.Add(brain, Time.timeSinceLevelLoad);
                }
            }
        }
        private void AddObjectInfluence()
        {
            for (int i = 0; i < ObjectInfluences.Length; i++)
            {
                StatInfluencerSO influencer = ScriptableObject.CreateInstance<StatInfluencerSO>();
                influencer.InteractionName = ObjectInfluences[i].statTemplate.name + " influencer from " + InteractionName + " (" + GetInstanceID() + ")";
                influencer.generator = gameObject;
                influencer.stat = ObjectInfluences[i].statTemplate;
                influencer.maxChange = ObjectInfluences[i].maxChange;
                influencer.duration = m_Duration;
                influencer.cooldown = m_Cooldown;

                m_StatsTracker.TryAddInfluencer(influencer);
            }
        }

        [Serializable]
        public struct StatInfluence
        {
            [SerializeField, Tooltip("The Stat this influencer acts upon.")]
            public StatSO statTemplate;
            [SerializeField, Tooltip("The maximum amount of change this influencer will impart upon the trait, to the limit of the stats allowable value.")]
            public float maxChange;
        }
    }
}