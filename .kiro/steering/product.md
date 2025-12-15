# Math Comic Generator Product Overview

Math Comic Generator (数学漫画生成器) is an educational web application that generates multi-panel comics to help children learn mathematics concepts. The application uses AI (Gemini API) to create engaging, age-appropriate visual content that makes math learning fun and accessible.

## Core Features

- **Math Concept Input**: Users input mathematical topics or problems
- **AI-Powered Comic Generation**: Creates multi-panel comics using Gemini API
- **Age-Appropriate Content**: Ensures generated content is suitable for children
- **Customizable Options**: Supports different age groups, difficulty levels, and panel counts
- **Content Management**: Stores and manages generated comics with metadata

## Target Audience

Primary users are educators, parents, and children learning mathematics through visual storytelling.

## Architecture

The application follows a clean architecture pattern with:
- **API Layer**: RESTful services for comic generation
- **Web Layer**: Blazor Server frontend for user interaction
- **Shared Layer**: Common models, interfaces, and business logic
- **External Integration**: Gemini API for AI-powered content generation

## Safety & Content Guidelines

All generated content must be child-safe, educational, and mathematically accurate.