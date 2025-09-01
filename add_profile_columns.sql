-- Add missing Profile columns
-- This script adds the new profile fields that were added to the Profile model

ALTER TABLE "Profiles" 
ADD COLUMN "Strengths" character varying(1000),
ADD COLUMN "Weaknesses" character varying(1000),
ADD COLUMN "CareerGoals" character varying(1000),
ADD COLUMN "Interests" character varying(1000);

-- Add comments to document the new columns
COMMENT ON COLUMN "Profiles"."Strengths" IS 'User strengths and skills';
COMMENT ON COLUMN "Profiles"."Weaknesses" IS 'Areas for improvement';
COMMENT ON COLUMN "Profiles"."CareerGoals" IS 'Career aspirations and goals';
COMMENT ON COLUMN "Profiles"."Interests" IS 'Professional interests and passions';

