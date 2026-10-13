#ifndef __INCLUDE_FUNGUS_DATA__
#define __INCLUDE_FUNGUS_DATA__


#define BLOCKING_BIT (1u << 31u)

#define FUNGAL_MASS (1023u)

#define CHEMO_ATTRACTANT (1023u << 10u)

bool IsCellAvailable(uint cell)
{
    return (cell & BLOCKING_BIT) == 0;
}

float GetFungalMass(uint cell)
{
    return saturate((cell & FUNGAL_MASS) / 1023.0);
}

float GetChemoAttractant(uint cell)
{
    return saturate(((cell & CHEMO_ATTRACTANT) >> 10) / 1023.0);
}

uint SetChemoAttractant(uint cell, uint attractant)
{
    return cell | (attractant << 10);
}

uint SetCell(float mass, float attractant)
{
    return ((uint) (saturate(mass) * FUNGAL_MASS) & FUNGAL_MASS) + (((uint)(saturate(attractant) * FUNGAL_MASS) & FUNGAL_MASS) << 10);
}

#endif