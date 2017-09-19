﻿ using System;
 using System.Collections.Generic;
 using System.Linq;
 using System.Numerics;
 using MiNET;
 using MiNET.Blocks;
 using MiNET.Entities;
 using MiNET.Entities.Projectiles;
 using MiNET.Net;
 using MiNET.Particles;
 using MiNET.Utils;
 using MiNET.Worlds;
 using SkyCore.Player;

namespace SkyCore.Games.Murder.Entities
{
	public class GunProjectile : Arrow
	{

		public GunProjectile(MiNET.Player shooter, Level level, int damage = 2, bool isCritical = false) : base(shooter, level, damage, isCritical)
		{
	
		}

		public override void OnTick()
		{
			//base.OnTick();

			if (KnownPosition.Y <= 0
				|| (Velocity.Length() <= 0 && DespawnOnImpact)
				|| (Velocity.Length() <= 0 && !DespawnOnImpact && Ttl <= 0))
			{
				if (DespawnOnImpact || (!DespawnOnImpact && Ttl <= 0))
				{
					DespawnEntity();
					return;
				}
				IsCritical = false;
				return;
			}

			Ttl--;

			if (KnownPosition.Y <= 0 || Velocity.Length() <= 0) return;

			Entity entityCollided = CheckEntityCollide(KnownPosition, Velocity);

			bool collided = false;
			if (entityCollided != null)
			{
				double speed = Math.Sqrt(Velocity.X * Velocity.X + Velocity.Y * Velocity.Y + Velocity.Z * Velocity.Z);
				double damage = Math.Ceiling(speed * Damage);
				if (IsCritical)
				{
					damage += Level.Random.Next((int)(damage / 2 + 2));

					McpeAnimate animate = McpeAnimate.CreateObject();
					animate.runtimeEntityId = entityCollided.EntityId;
					animate.actionId = 4;
					Level.RelayBroadcast(animate);
				}

				if (PowerLevel > 0)
				{
					damage = damage + ((PowerLevel + 1) * 0.25);
				}

				collided = true;

				SkyPlayer player = entityCollided as SkyPlayer;

				if (player != null)
				{
					if (player.IsGameSpectator)
					{
						collided = false;
					}
					else
					{
						damage = player.DamageCalculator.CalculatePlayerDamage(this, player, null, damage, DamageCause.Projectile);
						player.LastAttackTarget = entityCollided;
					}
				}

				if (collided)
				{
					entityCollided.HealthManager.TakeHit(this, (int)damage, DamageCause.Projectile);
					entityCollided.HealthManager.LastDamageSource = Shooter;

					DespawnEntity();
					return;
				}
			}

			var velocity2 = Velocity;
			velocity2 *= (float)(1.0d - Drag);
			velocity2 -= new Vector3(0, (float)Gravity, 0);
			double distance = velocity2.Length();
			velocity2 = Vector3.Normalize(velocity2) / 2;

			for (int i = 0; i < Math.Ceiling(distance) * 2; i++)
			{
				Vector3 nextPos = KnownPosition.ToVector3();
				nextPos.X += velocity2.X * i;
				nextPos.Y += velocity2.Y * i;
				nextPos.Z += velocity2.Z * i;

				Block block = Level.GetBlock(nextPos);
				collided = block.IsSolid && block.GetBoundingBox().Contains(nextPos);
				if (collided)
				{
					SetIntersectLocation(block.GetBoundingBox(), KnownPosition.ToVector3());
					break;
				}
			}

			if (collided)
			{
				Velocity = Vector3.Zero;
			}
			else
			{
				KnownPosition.X += Velocity.X;
				KnownPosition.Y += Velocity.Y;
				KnownPosition.Z += Velocity.Z;

				Velocity *= (float)(1.0 - Drag);
				Velocity -= new Vector3(0, (float)Gravity, 0);

				KnownPosition.Yaw = (float)Velocity.GetYaw();
				KnownPosition.Pitch = (float)Velocity.GetPitch();
			}

			// For debugging of flight-path
			if (BroadcastMovement)
			{
				//LastUpdatedTime = DateTime.UtcNow;

				BroadcastMoveAndMotion();
			}
		}

		private Entity CheckEntityCollide(Vector3 position, Vector3 direction)
		{
			Ray2 ray = new Ray2
			{
				x = position,
				d = Vector3.Normalize(direction)
			};

			var players = Level.GetSpawnedPlayers().OrderBy(player => Vector3.Distance(position, player.KnownPosition.ToVector3()));
			foreach (var entity in players)
			{
				if (entity == Shooter) continue;
				if (entity.GameMode == GameMode.Spectator) continue;

				if (Intersect(entity.GetBoundingBox() + HitBoxPrecision, ray))
				{
					if (ray.tNear > direction.Length()) break;

					Vector3 p = ray.x + new Vector3((float)ray.tNear) * ray.d;
					KnownPosition = new PlayerLocation(p.X, p.Y, p.Z);
					return entity;
				}
			}

			var entities = Level.Entities.Values.OrderBy(entity => Vector3.Distance(position, entity.KnownPosition.ToVector3()));
			foreach (Entity entity in entities)
			{
				if (entity == Shooter) continue;
				if (entity == this) continue;
				if (entity is Projectile) continue;

				if (Intersect(entity.GetBoundingBox() + HitBoxPrecision, ray))
				{
					if (ray.tNear > direction.Length()) break;

					Vector3 p = ray.x + new Vector3((float)ray.tNear) * ray.d;
					KnownPosition = new PlayerLocation(p.X, p.Y, p.Z);
					return entity;
				}
			}

			return null;
		}

		private bool CheckBlockCollide(PlayerLocation location)
		{
			var bbox = GetBoundingBox();
			var pos = location.ToVector3();

			var coords = new BlockCoordinates(
				(int)Math.Floor(KnownPosition.X),
				(int)Math.Floor((bbox.Max.Y + bbox.Min.Y) / 2.0),
				(int)Math.Floor(KnownPosition.Z));

			Dictionary<double, Block> blocks = new Dictionary<double, Block>();

			for (int x = -1; x < 2; x++)
			{
				for (int z = -1; z < 2; z++)
				{
					for (int y = -1; y < 2; y++)
					{
						Block block = Level.GetBlock(coords.X + x, coords.Y + y, coords.Z + z);
						if (block is Air) continue;

						BoundingBox blockbox = block.GetBoundingBox() + 0.3f;
						if (blockbox.Intersects(GetBoundingBox()))
						{
							//if (!blockbox.Contains(KnownPosition.ToVector3())) continue;

							if (block is FlowingLava || block is StationaryLava)
							{
								HealthManager.Ignite(1200);
								continue;
							}

							if (!block.IsSolid) continue;

							blockbox = block.GetBoundingBox();

							var midPoint = blockbox.Min + new Vector3(0.5f);
							blocks.Add(Vector3.Distance((pos - Velocity), midPoint), block);
						}
					}
				}
			}

			if (blocks.Count == 0) return false;

			var firstBlock = blocks.OrderBy(pair => pair.Key).First().Value;

			BoundingBox boundingBox = firstBlock.GetBoundingBox();
			if (!SetIntersectLocation(boundingBox, KnownPosition.ToVector3()))
			{
				// No real hit
				return false;
			}

			// Use to debug hits, makes visual impressions (can be used for paintball too)
			var substBlock = new Stone { Coordinates = firstBlock.Coordinates };
			Level.SetBlock(substBlock);
			// End debug block

			Velocity = Vector3.Zero;
			return true;
		}

		public new bool SetIntersectLocation(BoundingBox bbox, Vector3 location)
		{
			Ray ray = new Ray(location - Velocity, Vector3.Normalize(Velocity));
			double? distance = ray.Intersects(bbox);
			if (distance != null)
			{
				float dist = (float)distance - 0.1f;
				Vector3 pos = ray.Position + (ray.Direction * new Vector3(dist));
				KnownPosition.X = pos.X;
				KnownPosition.Y = pos.Y;
				KnownPosition.Z = pos.Z;
				return true;
			}

			return false;
		}

		/// <summary>
		/// For debugging of flight-path and rotation.
		/// </summary>
		private void BroadcastMoveAndMotion()
		{
			if (new Random().Next(5) == 0)
			{
				McpeSetEntityMotion motions = McpeSetEntityMotion.CreateObject();
				motions.runtimeEntityId = EntityId;
				motions.velocity = Velocity;
				//new Task(() => Level.RelayBroadcast(motions)).Start();
				Level.RelayBroadcast(motions);
			}

			McpeMoveEntity moveEntity = McpeMoveEntity.CreateObject();
			moveEntity.runtimeEntityId = EntityId;
			moveEntity.position = KnownPosition;
			Level.RelayBroadcast(moveEntity);

			if (Shooter != null && IsCritical)
			{
				var particle = new CriticalParticle(Level);
				particle.Position = KnownPosition.ToVector3();
				particle.Spawn(new[] { Shooter });
			}
		}

		public new bool BroadcastMovement { get; set; }

	}
}
