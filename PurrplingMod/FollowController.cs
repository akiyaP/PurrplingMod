﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using StardewValley.Locations;
using Microsoft.Xna.Framework;

namespace PurrplingMod
{
    public class FollowController
    {
        public const int FOLLOWING_LOST_TIMEOUT = 50;
        public Character leader;
        public NPC follower;
        public int followingLostTime = 0;
        public Stack<Point> pathToFollow;
        public Point currentFollowedPoint;
        public Point leaderLastTileCheckPoint;

        public FollowController()
        {
            this.pathToFollow = new Stack<Point>();
        }

        public void Update(GameTime time)
        {
            if (this.follower == null || this.leader == null)
                return;

            if (this.leader.currentLocation != this.follower.currentLocation || this.FollowerLeaderIsTooFar())
            {
                FollowController.WarpTo(this.follower, this.leader.currentLocation, this.leader.getTileLocationPoint());
                this.currentFollowedPoint = this.leaderLastTileCheckPoint = this.leader.getTileLocationPoint();
            }

            this.UpdateFollowing(time, this.follower, this.leader);
        }

        protected void UpdateFollowing(GameTime time, NPC follower, Character leader)
        {
            Point leaderTilePoint = leader.getTileLocationPoint();
            Point followerTilePoint = follower.getTileLocationPoint();
            Point leaderBoxCenter = leader.GetBoundingBox().Center;
            Point followerBoxCenter = follower.GetBoundingBox().Center;

            if (Helper.Distance(leaderTilePoint, followerTilePoint) > 7)
                this.follower.speed = this.leader.speed;

            if (follower.isMoving())
                this.followingLostTime = 0; // Follower move? Reset standing time
            else
            {
                if (Helper.Distance(leaderTilePoint, followerTilePoint) < 3)
                    return;
                if (this.followingLostTime++ >= FollowController.FOLLOWING_LOST_TIMEOUT)
                {
                    this.ResolveLostFollow();
                    return;
                }
            }

            if (leaderTilePoint.X != this.leaderLastTileCheckPoint.X && leaderTilePoint.Y != this.leaderLastTileCheckPoint.Y)
            {
                this.AddPathPoint(leaderTilePoint);
                this.leaderLastTileCheckPoint = leaderTilePoint;
            }

            if (Helper.Distance(leaderBoxCenter, followerBoxCenter) < 80)
            {
                follower.Halt();
                this.pathToFollow.Clear();
                this.currentFollowedPoint = leaderTilePoint;
                return;
            }

            if (this.pathToFollow.Count > 5 && Helper.Distance(leaderTilePoint, followerTilePoint) > 7)
            {
                this.ResolveLostFollow();
                return;
            }

            if (follower.controller == null)
                this.FollowPath(time);
        }

        private void ResolveLostFollow()
        {
            Point endTilePoint = this.leader.getTileLocationPoint();

            this.follower.Halt();
            this.follower.addedSpeed = 2;
            this.pathToFollow.Clear();
            this.currentFollowedPoint = endTilePoint;
            if (!FollowController.ComeTo(this.follower, endTilePoint))
                FollowController.WarpTo(this.follower, endTilePoint);
            this.followingLostTime = 0;
        }

        private void FollowPath(GameTime time)
        {
            if (this.pathToFollow.Count == 0)
                this.currentFollowedPoint = this.leader.getTileLocationPoint();
            else if (this.currentFollowedPoint == this.follower.getTileLocationPoint())
                this.currentFollowedPoint = this.pathToFollow.Pop();

            FollowController.FollowTile(this.follower, this.currentFollowedPoint, time);
        }

        private void AddPathPoint(Point p)
        {
            if (this.pathToFollow.Count == 0)
                this.currentFollowedPoint = p;
            this.pathToFollow.Push(p);
        }

        private bool FollowerLeaderIsTooFar()
        {
            return Helper.Distance(this.leader.getTileLocationPoint(), this.follower.getTileLocationPoint()) > 64;
        }

        public static void FollowTile(NPC follower, Point endPointTile, GameTime time)
        {
            Point startPointTile = follower.getTileLocationPoint();

            if (endPointTile.X < startPointTile.X)
                follower.SetMovingOnlyLeft();
            if (endPointTile.X > startPointTile.X)
                follower.SetMovingOnlyRight();
            if (endPointTile.Y < startPointTile.Y)
                follower.SetMovingOnlyUp();
            if (endPointTile.Y > startPointTile.Y)
                follower.SetMovingOnlyDown();

            follower.willDestroyObjectsUnderfoot = false;
            follower.MovePosition(time, Game1.viewport, follower.currentLocation);
        }

        public static bool ComeTo(NPC follower, Point endPointTile)
        {
            Point npcTilePosition = follower.getTileLocationPoint();
            Stack<Point> nearPoints;

            nearPoints = new Stack<Point>(
                Helper.SortPointsByNearest(
                    Helper.NearPoints(endPointTile, 1), npcTilePosition
                )
            );

            if (follower.controller == null)
            {
                follower.Halt();
                follower.temporaryController = null;

                Stack<Point> path = FollowController.FindFirstPath(npcTilePosition, nearPoints, follower.currentLocation, follower);
                follower.controller = new PathFindController(path, follower, follower.currentLocation);

                if (follower.controller.pathToEndPoint == null)
                    return false;
            }

            return true;
        }

        public static void WarpTo(NPC follower, Point tilePosition)
        {
            follower.Halt();
            follower.controller = follower.temporaryController = null;
            follower.setTilePosition(tilePosition);
        }

        public static void WarpTo(NPC follower, GameLocation location, Point tilePosition)
        {
            if (follower.currentLocation == location)
                FollowController.WarpTo(follower, tilePosition);

            follower.Halt();
            follower.controller = follower.temporaryController = null;
            follower.currentLocation.characters.Remove(follower);
            follower.currentLocation = location;
            follower.setTilePosition(tilePosition);

            location.addCharacter(follower);
        }

        public static Stack<Point> FindFirstPath(Point startTilePoint, Stack<Point> nearPointsStack, GameLocation location, NPC n, int limit = 100)
        {
            Stack<Point> path;
            PathFindController.isAtEnd endFunction = new PathFindController.isAtEnd(PathFindController.isAtEndPoint);

            while (nearPointsStack.Count > 0)
            {
                Point target = nearPointsStack.Pop();
                path = PathFindController.findPath(startTilePoint, target, endFunction, location, n, limit);
                if (path != null)
                    return path;
            }

            return null;
        }

    }
}
