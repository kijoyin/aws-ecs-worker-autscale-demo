# GitHub Repository Setup Guide

## Step 1: Create GitHub Repository

### Option A: Using GitHub Web Interface

1. **Go to GitHub**: Navigate to https://github.com/new

2. **Repository Settings**:
   - **Repository name**: `worker-autoscale`
   - **Description**: `.NET 10 Worker Service demonstrating AWS ECS auto-scaling with CloudWatch EMF custom metrics`
   - **Visibility**: ✅ Public
   - **DO NOT initialize** with README, .gitignore, or license (we already have these)

3. **Click**: "Create repository"

### Option B: Using GitHub CLI

```bash
# Install GitHub CLI if you haven't: https://cli.github.com/
gh auth login
gh repo create worker-autoscale --public --description ".NET 10 Worker Service demonstrating AWS ECS auto-scaling with CloudWatch EMF custom metrics"
```

## Step 2: Push to GitHub

After creating the repository on GitHub, run these commands:

```bash
# Add remote (replace YOUR_USERNAME with your GitHub username)
git remote add origin https://github.com/YOUR_USERNAME/worker-autoscale.git

# Verify remote
git remote -v

# Push to GitHub
git branch -M main
git push -u origin main
```

### If you're using SSH:
```bash
git remote add origin git@github.com:YOUR_USERNAME/worker-autoscale.git
git branch -M main
git push -u origin main
```

## Step 3: Verify Repository

1. Visit: `https://github.com/YOUR_USERNAME/worker-autoscale`
2. You should see:
   - ✅ README.md displayed on the home page
   - ✅ All project files
   - ✅ Complete documentation

## Step 4: Add Repository Topics (Optional but Recommended)

On GitHub, click "⚙️" next to "About" and add these topics:
- `dotnet`
- `dotnet10`
- `aws`
- `aws-ecs`
- `cloudwatch`
- `emf`
- `auto-scaling`
- `worker-service`
- `batch-processing`
- `metrics`
- `csharp`
- `docker`

## Step 5: Enable GitHub Pages (Optional)

If you want to host documentation:
1. Go to Settings → Pages
2. Source: Deploy from a branch
3. Branch: `main` / `docs`
4. Save

## Quick Reference Commands

```bash
# Check status
git status

# View commit history
git log --oneline

# View remote
git remote -v

# Push changes
git push

# Pull changes
git pull

# Create and switch to new branch
git checkout -b feature-branch-name

# Switch back to main
git checkout main
```

## Repository URL Structure

Once created, your repository will be at:
- **HTTPS**: `https://github.com/YOUR_USERNAME/worker-autoscale.git`
- **SSH**: `git@github.com:YOUR_USERNAME/worker-autoscale.git`
- **Web**: `https://github.com/YOUR_USERNAME/worker-autoscale`

## Recommended Repository Description

```
.NET 10 Worker Service demonstrating AWS ECS auto-scaling with CloudWatch EMF custom metrics. 
Perfect for blog posts and learning ECS auto-scaling with database-driven workloads. 
Includes complete deployment guide and mock database for easy testing.
```

## Recommended Repository Website

Add this to the "Website" field:
```
https://aws.amazon.com/ecs/
```

## Add Badges (Already in README.md)

The README.md already includes these badges:
- .NET version badge
- AWS ECS badge
- CloudWatch badge  
- MIT License badge

## Next Steps After Publishing

1. ✅ Star your own repository (for visibility)
2. ✅ Share on social media with relevant hashtags
3. ✅ Add link to your blog post when published
4. ✅ Consider adding GitHub Discussions for community Q&A
5. ✅ Add issues/labels for known limitations or future enhancements

## Common Issues

### Authentication Required
If you get authentication errors:
```bash
# For HTTPS, use Personal Access Token
gh auth login

# Or configure credential helper
git config --global credential.helper store
```

### Push Rejected
If push is rejected:
```bash
git pull origin main --rebase
git push -u origin main
```

### Wrong Remote URL
```bash
# Remove incorrect remote
git remote remove origin

# Add correct remote
git remote add origin https://github.com/YOUR_USERNAME/worker-autoscale.git
```

## Congratulations! 🎉

Your repository is now public and ready to share with the world!

Share it with:
- `#dotnet #aws #ecs #cloudwatch #autoscaling`
